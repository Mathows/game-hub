using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Uma TRANSIÇÃO de status da nota (de → para, com observação). É o padrão de auditoria
/// de fluxo do FinFix (HistoricoBoleto/LogStatusNotaFiscal): responde "por que a nota
/// está neste status?" e "quando/quem mudou?". O IAuditavel completa o quem/quando.
/// </summary>
public class HistoricoStatusNota : IAuditavel
{
    public int Id { get; set; }

    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    /// <summary>Status anterior (nulo na criação da nota).</summary>
    public StatusNotaFiscal? StatusAnterior { get; set; }
    public StatusNotaFiscal StatusNovo { get; set; }

    public string? Observacao { get; set; }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
