using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Implementa o direito de ACESSO da LGPD: junta, num único pacote, tudo o que a loja
/// guarda sobre o titular.
///
/// Note o desenho: cada consulta usa AsNoTracking (é leitura pura) e projeta para um
/// record de exportação. Nada de devolver a entidade crua — a entidade tem FKs,
/// campos de auditoria e navegações circulares que não interessam (nem podem ir) ao titular.
/// </summary>
public class DadosPessoaisService : IDadosPessoaisService
{
    private readonly GameHubDbContext _context;

    public DadosPessoaisService(GameHubDbContext context) => _context = context;

    public async Task<DadosPessoaisExportados> ExportarAsync(string applicationUserId)
    {
        const string aviso =
            "Dados que a LOJA mantém sobre você (cadastro, endereços, pedidos, notas fiscais, " +
            "aluguéis, trocas e propostas). Sua conta de login está na seção 'ContaDeLogin' " +
            "deste mesmo arquivo. Gerado a pedido do titular — LGPD art. 18, II.";

        var cliente = await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

        // Titular sem cadastro de cliente: devolve o pacote vazio, mas VÁLIDO.
        // (Quem só criou login e nunca comprou tem direito à resposta igualmente.)
        if (cliente is null)
        {
            return new DadosPessoaisExportados(
                DateTime.Now, aviso, null, [], [], [], [], []);
        }

        var enderecos = await _context.Enderecos.AsNoTracking()
            .Where(e => e.ClienteId == cliente.Id)
            .OrderByDescending(e => e.Principal)
            .Select(e => new EnderecoExportado(
                e.Cep, e.Logradouro, e.Numero, e.Complemento,
                e.Bairro, e.Cidade, e.Uf, e.Principal))
            .ToListAsync();

        // Pedidos: traz itens (nome do jogo), cupom e a nota fiscal ligada.
        // A nota entra porque o DOCUMENTO FISCAL também é dado do titular — ele tem
        // direito de saber que existe uma NF-e no nome dele.
        var pedidosDb = await _context.Pedidos.AsNoTracking()
            .Where(p => p.ClienteId == cliente.Id)
            .Include(p => p.Itens).ThenInclude(i => i.Jogo)
            .Include(p => p.Cupom)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var notasPorPedido = await _context.NotasFiscais.AsNoTracking()
            .Where(n => pedidosDb.Select(p => p.Id).Contains(n.PedidoId))
            .ToDictionaryAsync(n => n.PedidoId, n => $"NF-e {n.Numero}/{n.Serie} — {n.Status}");

        var pedidos = pedidosDb.Select(p => new PedidoExportado(
            p.Id,
            p.DataPedido,
            p.Status.ToString(),
            p.ValorTotal,
            p.ValorFrete,
            p.Desconto,
            p.Cupom?.Codigo,
            p.FormaPagamento?.ToString(),
            p.DataPagamento,
            p.EnderecoEntrega is null
                ? null
                : $"{p.EnderecoEntrega.Logradouro}, {p.EnderecoEntrega.Numero} — " +
                  $"{p.EnderecoEntrega.Bairro}, {p.EnderecoEntrega.Cidade}/{p.EnderecoEntrega.Uf} — " +
                  $"CEP {p.EnderecoEntrega.Cep}",
            notasPorPedido.TryGetValue(p.Id, out var nf) ? nf : null,
            p.Itens.Select(i => new ItemExportado(
                i.Jogo?.Titulo ?? $"jogo #{i.JogoId}", i.Quantidade, i.PrecoUnitario)).ToList()
        )).ToList();

        var alugueis = await _context.Alugueis.AsNoTracking()
            .Where(a => a.ClienteId == cliente.Id)
            .Include(a => a.Jogo)
            .OrderBy(a => a.Id)
            .Select(a => new AluguelExportado(
                a.Id, a.Jogo!.Titulo, a.DataInicio, a.DataPrevistaDevolucao,
                a.DataDevolucao, a.ValorTotal, a.Status.ToString()))
            .ToListAsync();

        // Trocas: o titular pode ser ofertante OU receptor — os dois lados são dado dele.
        var trocas = await _context.Trocas.AsNoTracking()
            .Where(t => t.ClienteOfertanteId == cliente.Id || t.ClienteReceptorId == cliente.Id)
            .Include(t => t.JogoOferecido)
            .Include(t => t.JogoDesejado)
            .OrderBy(t => t.Id)
            .Select(t => new TrocaExportada(
                t.Id, t.JogoOferecido!.Titulo, t.JogoDesejado!.Titulo,
                t.DataProposta, t.Status.ToString()))
            .ToListAsync();

        var propostas = await _context.PropostasVenda.AsNoTracking()
            .Where(pv => pv.ClienteId == cliente.Id)
            .Include(pv => pv.Jogo)
            .OrderBy(pv => pv.Id)
            .Select(pv => new PropostaExportada(
                pv.Id, pv.Jogo!.Titulo, pv.Condicao.ToString(), pv.ValorPedido,
                pv.ValorAprovado, pv.Status.ToString(), pv.CriadoEm))
            .ToListAsync();

        return new DadosPessoaisExportados(
            DateTime.Now,
            aviso,
            new ClienteExportado(
                cliente.Nome, cliente.Telefone, cliente.CpfCnpj,
                cliente.TipoPessoa.ToString(), cliente.DataCadastro),
            enderecos, pedidos, alugueis, trocas, propostas);
    }

    // =======================================================================
    // DIREITO DE EXCLUSÃO — por anonimização
    // =======================================================================

    public async Task<ResultadoAnonimizacao> AnonimizarAsync(string applicationUserId)
    {
        var acoes = new List<string>();

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

        if (cliente is null)
        {
            return new ResultadoAnonimizacao(0, 0, 0, 0,
                ["Nenhum cadastro de cliente encontrado — só a conta de login foi encerrada."]);
        }

        // TRANSAÇÃO: anonimizar é uma operação de VÁRIAS tabelas. Se falhar no meio,
        // ficaríamos com um cliente sem nome mas com endereços intactos — pior que não
        // ter começado. Ou tudo, ou nada.
        await using var transacao = await _context.Database.BeginTransactionAsync();

        // ---- 1. O CADASTRO: sobrescrever, não apagar ----
        // A linha continua (os pedidos apontam para ela via FK), mas nada nela identifica
        // uma pessoa. É isto que o art. 12 chama de dado anonimizado.
        cliente.Nome = $"Titular removido #{cliente.Id}";
        cliente.Telefone = null;
        cliente.CpfCnpj = null;
        // Desliga do login: mesmo que alguém recrie uma conta com o mesmo e-mail,
        // não "herda" o cadastro antigo.
        cliente.ApplicationUserId = null;
        // A flag NÃO é o que protege (as linhas acima são) — ela diz ao sistema que este
        // cadastro é uma exclusão legítima, não um registro corrompido.
        cliente.Anonimizado = true;
        cliente.AnonimizadoEm = DateTime.Now;
        acoes.Add("Cadastro anonimizado (nome, telefone e CPF/CNPJ removidos) e marcado como inativo.");

        // ---- 2. A AGENDA DE ENDEREÇOS: apagar de verdade ----
        // Aqui não há obrigação de guarda: a agenda é conveniência do cliente,
        // não documento fiscal. Sem base legal para conservar → apaga.
        var enderecos = await _context.Enderecos.Where(e => e.ClienteId == cliente.Id).ToListAsync();
        _context.Enderecos.RemoveRange(enderecos);
        acoes.Add($"{enderecos.Count} endereço(s) da agenda apagado(s) definitivamente.");

        // ---- 3. OS PEDIDOS: a decisão é POR REGISTRO, conforme a base legal de cada um ----
        var pedidos = await _context.Pedidos
            .Where(p => p.ClienteId == cliente.Id)
            .ToListAsync();

        var pedidosComNota = await _context.NotasFiscais
            .Where(n => n.Status == StatusNotaFiscal.Autorizada)
            .Select(n => n.PedidoId)
            .ToListAsync();

        var notasMantidas = pedidos.Count(p => pedidosComNota.Contains(p.Id));
        var enderecosLimpos = 0;

        foreach (var pedido in pedidos)
        {
            // Pedido COM nota fiscal autorizada: o endereço de entrega faz parte de um
            // DOCUMENTO FISCAL já emitido → conservado (art. 16, I: obrigação legal).
            if (pedidosComNota.Contains(pedido.Id)) continue;

            // Pedido SEM nota: nada obriga a guardar o endereço → o snapshot é limpo.
            if (pedido.EnderecoEntrega is not null)
            {
                pedido.EnderecoEntrega = null;
                enderecosLimpos++;
            }
        }

        acoes.Add($"{pedidos.Count} pedido(s) MANTIDO(S) — valores, itens e datas ficam por " +
                  $"obrigação fiscal (guarda de 5 anos), mas sem identificar você.");
        acoes.Add($"{notasMantidas} nota(s) fiscal(is) autorizada(s) mantida(s) — documento fiscal " +
                  "não pode ser destruído a pedido do titular.");
        if (enderecosLimpos > 0)
            acoes.Add($"{enderecosLimpos} endereço(s) de entrega limpo(s) em pedidos SEM nota fiscal " +
                      "(não havia obrigação legal para conservá-los).");

        // ---- 4. TEXTO LIVRE: onde dado pessoal se esconde ----
        // Campos que a pessoa digita são o ponto cego de qualquer anonimização: ninguém
        // sabe o que ela escreveu ali ("meu telefone é...", "entregar na casa da minha mãe").
        // Sem como auditar o conteúdo, o caminho seguro é limpar.
        var propostas = await _context.PropostasVenda
            .Where(pv => pv.ClienteId == cliente.Id && pv.ObservacaoCliente != null)
            .ToListAsync();
        foreach (var pv in propostas) pv.ObservacaoCliente = null;
        if (propostas.Count > 0)
            acoes.Add($"{propostas.Count} observação(ões) escrita(s) por você em propostas de venda apagada(s).");

        // ---- 5. A PROVA de que atendemos ----
        _context.RegistrosAnonimizacao.Add(new RegistroAnonimizacao
        {
            ClienteId = cliente.Id,
            SolicitadoEm = DateTime.Now,
            ResumoAcoes = string.Join(" | ", acoes),
            PedidosMantidos = pedidos.Count,
            NotasMantidas = notasMantidas
        });

        await _context.SaveChangesAsync();
        await transacao.CommitAsync();

        return new ResultadoAnonimizacao(
            enderecos.Count, pedidos.Count, notasMantidas, enderecosLimpos, acoes);
    }
}
