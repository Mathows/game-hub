using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Cupom de desconto aplicado no CHECKOUT (sobre o subtotal das COMPRAS do pedido).
///
/// Segurança essencial: a tela envia apenas o CÓDIGO; quem valida e CALCULA o desconto
/// é sempre o servidor (PedidoService) — nunca confiar num valor de desconto vindo do
/// cliente. As regras de validade/cálculo moram AQUI (Domain).
/// </summary>
public class Cupom : IAuditavel
{
    public int Id { get; set; }

    /// <summary>Código digitado pelo cliente (guardado em MAIÚSCULAS; único no banco).</summary>
    public string Codigo { get; set; } = string.Empty;

    public TipoDesconto Tipo { get; set; } = TipoDesconto.Percentual;

    /// <summary>Percentual (ex.: 10 = 10%) ou valor em R$ (ex.: 20 = R$ 20,00), conforme o Tipo.</summary>
    public decimal Valor { get; set; }

    /// <summary>Vale até esta data/hora.</summary>
    public DateTime Validade { get; set; }

    /// <summary>Máximo de usos (null = ilimitado).</summary>
    public int? LimiteUsos { get; set; }

    /// <summary>Quantas vezes já foi usado (incrementado pelo servidor, na transação do pedido).</summary>
    public int Usos { get; set; }

    /// <summary>Interruptor manual (desativar preserva o histórico, igual à Promoção).</summary>
    public bool Ativo { get; set; } = true;

    /// <summary>O cupom pode ser usado NESTE momento?</summary>
    public bool ValidoEm(DateTime momento) =>
        Ativo && momento <= Validade && (LimiteUsos is null || Usos < LimiteUsos);

    /// <summary>Calcula o desconto sobre um subtotal (nunca maior que o próprio subtotal).</summary>
    public decimal CalcularDesconto(decimal subtotal)
    {
        var desconto = Tipo == TipoDesconto.Percentual
            ? Math.Round(subtotal * (Valor / 100m), 2)
            : Valor;
        return Math.Min(desconto, subtotal);   // desconto não deixa o total negativo
    }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
