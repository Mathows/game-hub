using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// A COBRANÇA do pedido — o "como pagar" materializado, gerado no checkout conforme a
/// forma escolhida: PIX tem o copia-e-cola, boleto tem linha digitável + vencimento.
/// 1:1 com o Pedido (índice único), como a NotaFiscal — o padrão §5.1 de novo.
/// No FinFix é a tabela Boleto + NossoNumero; aqui é a versão didática unificada.
/// </summary>
public class Cobranca : IAuditavel
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public FormaPagamento Forma { get; set; }

    /// <summary>Payload PIX "copia e cola" (formato BR Code) — só quando Forma = Pix.</summary>
    public string? PixCopiaECola { get; set; }

    /// <summary>Linha digitável do boleto (47 dígitos) — só quando Forma = Boleto.</summary>
    public string? BoletoLinhaDigitavel { get; set; }
    public DateTime? BoletoVencimento { get; set; }

    /// <summary>Instrução exibida ao cliente (ex.: "aguardando aprovação do cartão").</summary>
    public string? Instrucao { get; set; }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
