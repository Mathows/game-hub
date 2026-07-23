using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Tabela de LOOKUP dos motivos de movimentação manual de estoque (ex.: "Compra de
/// fornecedor" na entrada; "Produto danificado" na saída).
///
/// É TABELA (e não enum) de propósito: o admin pode CADASTRAR motivos novos sem
/// recompilar o sistema — a lista é dado editável (Sistema.md §5.2). Cada motivo
/// pertence a UMA operação (Entrada ou Saída), e o formulário filtra por ela.
/// </summary>
public class MotivoMovimentacao : IAuditavel
{
    public int Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    /// <summary>A qual direção este motivo pertence (motivos de entrada ≠ de saída).</summary>
    public OperacaoEstoque Operacao { get; set; }

    /// <summary>Desativar tira das opções novas sem apagar (histórico preservado).</summary>
    public bool Ativo { get; set; } = true;

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
