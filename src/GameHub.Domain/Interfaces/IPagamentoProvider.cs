using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Domain.Interfaces;

/// <summary>Os dados de cobrança que o provider gera (o que o cliente usa pra pagar).</summary>
public record DadosCobranca(string? PixCopiaECola, string? BoletoLinhaDigitavel,
    DateTime? BoletoVencimento, string? Instrucao);

/// <summary>
/// O "lado gateway/banco" da cobrança — PROVIDER PLUGÁVEL (Sistema.md §4.1), como o da
/// nota fiscal: hoje simulado (gera PIX/boleto fake em formato realista); amanhã o sandbox
/// do Mercado Pago ou um banco — trocando só o registro na DI.
/// </summary>
public interface IPagamentoProvider
{
    Task<DadosCobranca> GerarCobrancaAsync(Pedido pedido, FormaPagamento forma);
}
