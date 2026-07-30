using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>Extrato e ajuste manual de estoque (EF Core, Scoped).</summary>
public class EstoqueService : IEstoqueService
{
    private readonly GameHubDbContext _context;

    public EstoqueService(GameHubDbContext context) => _context = context;

    public async Task<List<MovimentacaoEstoque>> ExtratoAsync(int jogoId)
        => await _context.MovimentacoesEstoque.AsNoTracking()
            .Include(m => m.Motivo)               // o motivo estruturado vem junto
            .Where(m => m.JogoId == jogoId)
            .OrderByDescending(m => m.Id)         // mais recente primeiro (como extrato bancário)
            .ToListAsync();

    public async Task<List<MotivoMovimentacao>> ObterMotivosAsync(OperacaoEstoque operacao)
        => await _context.MotivosMovimentacao.AsNoTracking()
            .Where(m => m.Ativo && m.Operacao == operacao)
            .OrderBy(m => m.Descricao)
            .ToListAsync();

    public async Task CadastrarMotivoAsync(string descricao, OperacaoEstoque operacao)
    {
        var texto = (descricao ?? string.Empty).Trim();
        if (texto.Length < 3)
            throw new InvalidOperationException("Descreva o motivo (mínimo 3 caracteres).");

        var jaExiste = await _context.MotivosMovimentacao
            .AnyAsync(m => m.Operacao == operacao && m.Descricao == texto);
        if (jaExiste)
            throw new InvalidOperationException("Já existe um motivo com essa descrição nesta operação.");

        _context.MotivosMovimentacao.Add(new MotivoMovimentacao { Descricao = texto, Operacao = operacao });
        await _context.SaveChangesAsync();   // auditoria automática registra quem cadastrou
    }

    public async Task AjustarAsync(int jogoId, int quantidade, int motivoId, string? observacao)
    {
        if (quantidade == 0)
            throw new InvalidOperationException("Informe uma quantidade diferente de zero.");

        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            var jogo = await _context.Jogos.FirstOrDefaultAsync(j => j.Id == jogoId)
                ?? throw new InvalidOperationException($"Jogo {jogoId} não encontrado.");

            // Motivo obrigatório e coerente com a direção (validado no SERVIDOR, como sempre):
            // um motivo de Entrada não explica uma Saída — e vice-versa.
            var motivo = await _context.MotivosMovimentacao.FirstOrDefaultAsync(m => m.Id == motivoId && m.Ativo)
                ?? throw new InvalidOperationException("Escolha um motivo válido.");

            var operacaoDoAjuste = quantidade > 0 ? OperacaoEstoque.Entrada : OperacaoEstoque.Saida;
            if (motivo.Operacao != operacaoDoAjuste)
                throw new InvalidOperationException(
                    $"O motivo \"{motivo.Descricao}\" é de {motivo.Operacao}, mas o ajuste é de {operacaoDoAjuste}.");

            if (jogo.QuantidadeEstoque + quantidade < 0)
                throw new InvalidOperationException(
                    $"O ajuste deixaria o estoque negativo (tem {jogo.QuantidadeEstoque}, ajuste {quantidade}).");

            jogo.QuantidadeEstoque += quantidade;
            jogo.Disponivel = jogo.QuantidadeEstoque > 0;

            _context.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                Jogo = jogo,
                Tipo = quantidade > 0 ? TipoMovimentacaoEstoque.Entrada : TipoMovimentacaoEstoque.Ajuste,
                Quantidade = quantidade,
                EstoqueDepois = jogo.QuantidadeEstoque,
                Motivo = motivo,
                Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim()
            });

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }
}
