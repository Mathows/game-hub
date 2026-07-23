using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Implementação do fechamento de compra com EF Core. Recebe o GameHubDbContext por
/// Injeção de Dependência (Scoped). O coração aqui é a TRANSAÇÃO: várias gravações
/// (criar cliente, criar pedido, criar itens, baixar estoque) precisam acontecer
/// como um bloco único — "tudo ou nada".
/// </summary>
public class PedidoService : IPedidoService
{
    private readonly GameHubDbContext _context;

    public PedidoService(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task<Pedido> FinalizarCompraAsync(string applicationUserId, string nomeCliente, IReadOnlyList<ItemCompra> itens, EnderecoEntrega? enderecoEntrega, string? cupomCodigo = null)
    {
        if (itens is null || itens.Count == 0)
            throw new InvalidOperationException("Não há itens de compra no carrinho.");

        // TRANSAÇÃO: abre um "envelope" no banco. Enquanto não damos Commit, nada é
        // definitivo. Se qualquer passo lançar exceção (ex.: estoque insuficiente),
        // o Rollback desfaz TODAS as gravações — nunca sobra um pedido pela metade.
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            // --- Passo 0: achar (ou criar) o Cliente ligado ao usuário logado ---
            // É a "ponte" entre o login (AspNetUsers) e a loja: ligamos pelo ApplicationUserId.
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

            if (cliente is null)
            {
                cliente = new Cliente { Nome = nomeCliente, ApplicationUserId = applicationUserId };
                _context.Clientes.Add(cliente);   // primeiro pedido do usuário: cria o cadastro dele
            }

            var pedido = new Pedido
            {
                Cliente = cliente,
                DataPedido = DateTime.Now,
                Status = StatusPedido.Pendente,    // vira "Pago" via webhook (Passo 4)
                EnderecoEntrega = enderecoEntrega  // SNAPSHOT do endereço no momento da compra
            };

            decimal total = 0m;

            var agora = DateTime.Now;
            foreach (var item in itens)
            {
                // Traz as promoções vigentes junto: o preço cobrado é decidido AQUI (servidor),
                // nunca o que veio da tela — segurança básica de e-commerce.
                var jogo = await _context.Jogos
                    .Include(j => j.Promocoes.Where(p => p.Ativa && p.Inicio <= agora && agora <= p.Fim))
                    .FirstOrDefaultAsync(j => j.Id == item.JogoId)
                    ?? throw new InvalidOperationException($"Jogo {item.JogoId} não encontrado.");

                // Regra de negócio: não vender mais do que tem em estoque.
                if (jogo.QuantidadeEstoque < item.Quantidade)
                    throw new InvalidOperationException(
                        $"Estoque insuficiente para \"{jogo.Titulo}\" (tem {jogo.QuantidadeEstoque}, pediu {item.Quantidade}).");

                jogo.QuantidadeEstoque -= item.Quantidade;   // BAIXA DE ESTOQUE

                // EXTRATO: a baixa vira uma linha de movimentação, na MESMA transação —
                // ou grava pedido + baixa + extrato, ou nada (nunca extrato "furado").
                _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    Jogo = jogo,
                    Tipo = TipoMovimentacaoEstoque.Venda,
                    Quantidade = -item.Quantidade,               // saída = negativa
                    EstoqueDepois = jogo.QuantidadeEstoque,      // o "saldo" após o movimento
                    Pedido = pedido,                             // referência (EF preenche o PedidoId ao salvar)
                    Observacao = "Venda"
                });

                var itemPedido = new ItemPedido
                {
                    Jogo = jogo,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = jogo.PrecoVigente(agora)  // preço VIGENTE, congelado no momento da compra
                };
                pedido.Itens.Add(itemPedido);
                total += itemPedido.PrecoUnitario * itemPedido.Quantidade;
            }

            // ---- Cupom: a validação QUE VALE (dentro da transação; a prévia do carrinho
            // é só cortesia). A tela mandou o CÓDIGO — o desconto é calculado AQUI. ----
            if (!string.IsNullOrWhiteSpace(cupomCodigo))
            {
                var codigo = cupomCodigo.Trim().ToUpperInvariant();
                var cupom = await _context.Cupons.FirstOrDefaultAsync(c => c.Codigo == codigo)
                    ?? throw new InvalidOperationException($"Cupom \"{codigo}\" não encontrado.");

                if (!cupom.ValidoEm(agora))
                    throw new InvalidOperationException($"O cupom \"{codigo}\" não está mais válido.");

                pedido.Cupom = cupom;
                pedido.Desconto = cupom.CalcularDesconto(total);   // desconto CONGELADO (snapshot)
                cupom.Usos++;                                      // consome 1 uso (na mesma transação!)
                total -= pedido.Desconto;
            }

            pedido.ValorTotal = total;
            _context.Pedidos.Add(pedido);

            await _context.SaveChangesAsync();   // gera os INSERTs/UPDATEs (ainda dentro da transação)
            await transacao.CommitAsync();       // CONFIRMA tudo de uma vez
            return pedido;
        }
        catch
        {
            await transacao.RollbackAsync();     // qualquer falha: desfaz tudo
            throw;                               // repassa o erro para a tela mostrar a mensagem
        }
    }

    public async Task<List<Pedido>> ObterPorUsuarioAsync(string applicationUserId)
        => await _context.Pedidos
            .AsNoTracking()                                   // leitura pura: não rastreia (traz sempre dados FRESCOS do banco)
            .Include(p => p.Itens).ThenInclude(i => i.Jogo)   // traz os itens e o jogo de cada um
            .Where(p => p.Cliente!.ApplicationUserId == applicationUserId)
            .OrderByDescending(p => p.DataPedido)
            .ToListAsync();

    public async Task<Pedido?> ObterPorIdAsync(int pedidoId, string applicationUserId)
        => await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
            .Include(p => p.Itens).ThenInclude(i => i.Jogo)
            .FirstOrDefaultAsync(p => p.Id == pedidoId && p.Cliente!.ApplicationUserId == applicationUserId);
}
