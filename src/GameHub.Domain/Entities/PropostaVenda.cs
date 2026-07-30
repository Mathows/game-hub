using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Proposta de VENDA do cliente para a loja: "quero vender meu jogo usado por R$ X".
/// Segue um WORKFLOW DE APROVAÇÃO (herdado do PedidoOnline legado): o cliente propõe,
/// o admin avalia e aprova (podendo contrapor o valor) ou recusa (com motivo).
/// Aprovada → o jogo ENTRA no estoque via extrato (P3) e o cliente recebe o crédito.
/// </summary>
public class PropostaVenda : IAuditavel
{
    public int Id { get; set; }

    // Quem está vendendo.
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    // Qual jogo do catálogo o cliente está oferecendo (a cópia dele, usada ou nova).
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    public CondicaoJogo Condicao { get; set; } = CondicaoJogo.Usado;

    /// <summary>Quanto o cliente PEDIU pela cópia.</summary>
    public decimal ValorPedido { get; set; }

    /// <summary>Quanto a loja PAGOU de fato (definido pelo admin na aprovação — pode contrapor).</summary>
    public decimal? ValorAprovado { get; set; }

    public StatusPropostaVenda Status { get; set; } = StatusPropostaVenda.Proposta;

    /// <summary>Observação do cliente (estado do jogo, caixa, manual...).</summary>
    public string? ObservacaoCliente { get; set; }

    /// <summary>Resposta do admin (motivo da recusa ou comentário da aprovação).</summary>
    public string? RespostaAdmin { get; set; }

    /// <summary>Quando a loja respondeu (aprovou/recusou).</summary>
    public DateTime? DataResposta { get; set; }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
