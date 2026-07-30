using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Implementação do workflow de venda pra loja (EF Core, Scoped).
/// O coração é a MÁQUINA DE ESTADOS: cada método confere o estado atual antes de
/// transicionar — um botão "Aprovar" clicado numa proposta já cancelada é recusado AQUI.
/// </summary>
public class PropostaVendaService : IPropostaVendaService
{
    private readonly GameHubDbContext _context;

    public PropostaVendaService(GameHubDbContext context) => _context = context;

    public async Task<PropostaVenda> PropoAsync(string applicationUserId, string nomeCliente,
        int jogoId, CondicaoJogo condicao, decimal valorPedido, string? observacao)
    {
        if (valorPedido <= 0)
            throw new InvalidOperationException("Informe quanto você quer pela cópia.");

        var jogo = await _context.Jogos.FirstOrDefaultAsync(j => j.Id == jogoId)
            ?? throw new InvalidOperationException("Escolha um jogo válido.");

        // Get-or-create do Cliente (a ponte login ↔ loja, como no PedidoService).
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);
        if (cliente is null)
        {
            cliente = new Cliente { Nome = nomeCliente, ApplicationUserId = applicationUserId };
            _context.Clientes.Add(cliente);
        }

        var proposta = new PropostaVenda
        {
            Cliente = cliente,
            Jogo = jogo,
            Condicao = condicao,
            ValorPedido = valorPedido,
            ObservacaoCliente = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            Status = StatusPropostaVenda.Proposta
        };
        _context.PropostasVenda.Add(proposta);
        await _context.SaveChangesAsync();
        return proposta;
    }

    public async Task<List<PropostaVenda>> MinhasAsync(string applicationUserId)
        => await _context.PropostasVenda.AsNoTracking()
            .Include(p => p.Jogo)
            .Where(p => p.Cliente!.ApplicationUserId == applicationUserId)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

    public async Task CancelarAsync(int propostaId, string applicationUserId)
    {
        // Só o DONO cancela, e só enquanto a loja ainda não pegou pra avaliar.
        var proposta = await _context.PropostasVenda
            .FirstOrDefaultAsync(p => p.Id == propostaId && p.Cliente!.ApplicationUserId == applicationUserId)
            ?? throw new InvalidOperationException("Proposta não encontrada.");

        if (proposta.Status != StatusPropostaVenda.Proposta)
            throw new InvalidOperationException($"Não dá mais para cancelar: a proposta está \"{proposta.Status}\".");

        proposta.Status = StatusPropostaVenda.Cancelada;
        await _context.SaveChangesAsync();
    }

    public async Task<List<PropostaVenda>> TodasAsync()
        => await _context.PropostasVenda.AsNoTracking()
            .Include(p => p.Jogo)
            .Include(p => p.Cliente)
            .OrderBy(p => p.Status == StatusPropostaVenda.Proposta || p.Status == StatusPropostaVenda.EmAvaliacao ? 0 : 1)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

    public async Task IniciarAvaliacaoAsync(int propostaId)
    {
        var proposta = await Obter(propostaId);
        // Transição válida: Proposta → EmAvaliacao. Qualquer outra origem é erro.
        if (proposta.Status != StatusPropostaVenda.Proposta)
            throw new InvalidOperationException($"Só propostas novas entram em avaliação (esta está \"{proposta.Status}\").");

        proposta.Status = StatusPropostaVenda.EmAvaliacao;
        await _context.SaveChangesAsync();
    }

    public async Task<PropostaVenda> AprovarAsync(int propostaId, decimal valorAprovado, string? resposta)
    {
        if (valorAprovado <= 0)
            throw new InvalidOperationException("Informe o valor que a loja vai pagar.");

        // TRANSAÇÃO: aprovar + entrada no estoque + extrato = tudo ou nada.
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            var proposta = await Obter(propostaId, incluirTudo: true);

            if (proposta.Status is not (StatusPropostaVenda.Proposta or StatusPropostaVenda.EmAvaliacao))
                throw new InvalidOperationException($"Esta proposta não pode ser aprovada (está \"{proposta.Status}\").");

            proposta.Status = StatusPropostaVenda.Aprovada;
            proposta.ValorAprovado = valorAprovado;
            proposta.RespostaAdmin = string.IsNullOrWhiteSpace(resposta) ? null : resposta.Trim();
            proposta.DataResposta = DateTime.Now;

            // A cópia comprada ENTRA no estoque — e o P3 garante o rastro no extrato.
            var jogo = proposta.Jogo!;
            jogo.QuantidadeEstoque += 1;
            jogo.Disponivel = true;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                Jogo = jogo,
                Tipo = TipoMovimentacaoEstoque.Entrada,
                Quantidade = +1,
                EstoqueDepois = jogo.QuantidadeEstoque,
                Observacao = $"Compra de cliente — proposta #{proposta.Id} (R$ {valorAprovado:N2})"
            });

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return proposta;
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }

    public async Task<PropostaVenda> RecusarAsync(int propostaId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Informe o motivo da recusa (o cliente merece saber por quê).");

        var proposta = await Obter(propostaId, incluirTudo: true);

        if (proposta.Status is not (StatusPropostaVenda.Proposta or StatusPropostaVenda.EmAvaliacao))
            throw new InvalidOperationException($"Esta proposta não pode ser recusada (está \"{proposta.Status}\").");

        proposta.Status = StatusPropostaVenda.Recusada;
        proposta.RespostaAdmin = motivo.Trim();
        proposta.DataResposta = DateTime.Now;
        await _context.SaveChangesAsync();
        return proposta;
    }

    private async Task<PropostaVenda> Obter(int id, bool incluirTudo = false)
    {
        var query = _context.PropostasVenda.AsQueryable();
        if (incluirTudo) query = query.Include(p => p.Jogo).Include(p => p.Cliente);
        return await query.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Proposta não encontrada.");
    }
}
