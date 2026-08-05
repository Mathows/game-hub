using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Nota fiscal do pedido — agora um REGISTRO PERSISTIDO (não mais gerada "na hora de ver",
/// como na Fase 4). Emitida quando o pedido é pago, com número sequencial, chave de acesso
/// e ciclo de status auditado (blueprint herdado do FinFix, reconstruído em Code First).
///
/// Relacionamento 1:1 com o Pedido garantido PELO SCHEMA (índice único em PedidoId) —
/// a mesma lição do endereço (§5.1): "exatamente um" é regra de banco, não de código.
/// </summary>
public class NotaFiscal : IAuditavel
{
    public int Id { get; set; }

    // 1 pedido → 1 nota (índice ÚNICO no banco).
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    /// <summary>Número sequencial da nota (por série) — o "NossoNumero" fiscal do FinFix.</summary>
    public int Numero { get; set; }
    public int Serie { get; set; } = 1;

    /// <summary>Chave de acesso de 44 dígitos (preenchida quando autorizada).</summary>
    public string? ChaveAcesso { get; set; }

    /// <summary>Protocolo de autorização devolvido pelo emissor.</summary>
    public string? Protocolo { get; set; }

    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Pendente;

    public decimal ValorTotal { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime? DataAutorizacao { get; set; }

    /// <summary>Por que o emissor rejeitou (o cliente merece saber o que corrigir).</summary>
    public string? MotivoRejeicao { get; set; }

    // Links dos ARQUIVOS no emissor (o PlugNotas devolve no resumo): o XML é o documento
    // fiscal OFICIAL (o que o contador escritura); o PDF é a DANFE (representação visual).
    // O download pro cliente passa pelo NOSSO endpoint autenticado (valida o dono) — o
    // jeito moderno do VisualizarNFe.ashx do legado.
    public string? UrlXml { get; set; }
    public string? UrlPdf { get; set; }

    // Toda transição de status vira uma linha aqui (o LogStatusNotaFiscal do FinFix).
    public ICollection<HistoricoStatusNota> Historico { get; set; } = new List<HistoricoStatusNota>();

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
