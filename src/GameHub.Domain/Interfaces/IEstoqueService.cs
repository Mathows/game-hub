using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Domain.Interfaces;

/// <summary>Extrato de estoque + ajuste manual (admin).</summary>
public interface IEstoqueService
{
    /// <summary>Extrato do jogo (mais recente primeiro), com o motivo carregado.</summary>
    Task<List<MovimentacaoEstoque>> ExtratoAsync(int jogoId);

    /// <summary>Motivos ATIVOS de uma operação (para o formulário filtrar Entrada/Saída).</summary>
    Task<List<MotivoMovimentacao>> ObterMotivosAsync(OperacaoEstoque operacao);

    /// <summary>Cadastra um motivo novo na tabela de lookup (a lista é editável pelo admin).</summary>
    Task CadastrarMotivoAsync(string descricao, OperacaoEstoque operacao);

    /// <summary>
    /// Ajuste manual do admin: <paramref name="quantidade"/> COM SINAL (+ entra, − sai),
    /// com um motivo da tabela (obrigatório) e observação livre (opcional). Atualiza o
    /// estoque E registra a linha do extrato numa transação. O servidor valida que o
    /// motivo pertence à operação certa (motivo de entrada não serve pra saída).
    /// </summary>
    Task AjustarAsync(int jogoId, int quantidade, int motivoId, string? observacao);
}
