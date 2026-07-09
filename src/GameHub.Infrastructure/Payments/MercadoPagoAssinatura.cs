using System.Security.Cryptography;
using System.Text;

namespace GameHub.Infrastructure.Payments;

/// <summary>
/// Gera e valida a assinatura do webhook no formato do <b>Mercado Pago</b>.
///
/// Como funciona (é o que torna o webhook SEGURO):
/// 1. O MP manda o cabeçalho <c>x-signature</c> assim: <c>ts=1700000000,v1=abc123...</c>
/// 2. O <c>v1</c> é um <b>HMAC-SHA256</b> de um "manifesto" — um texto padronizado
///    montado com o id do pagamento, o request-id e o ts.
/// 3. Nós recalculamos o mesmo HMAC com o NOSSO segredo. Se bater com o v1 recebido,
///    a chamada veio mesmo do MP (ninguém de fora sabe o segredo para forjar o hash).
/// </summary>
public static class MercadoPagoAssinatura
{
    /// <summary>Monta o manifesto EXATAMENTE no formato que o MP assina.</summary>
    public static string MontarManifesto(string dataId, string requestId, string ts)
        => $"id:{dataId};request-id:{requestId};ts:{ts};";

    /// <summary>Calcula o HMAC-SHA256 (hex minúsculo) do manifesto usando o segredo.</summary>
    public static string GerarHash(string segredo, string manifesto)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(segredo));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifesto));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Confere se o v1 recebido bate com o que calculamos.
    /// Usa comparação em tempo constante (FixedTimeEquals) para não vazar informação por timing.
    /// </summary>
    public static bool Validar(string segredo, string dataId, string requestId, string ts, string? v1Recebido)
    {
        if (string.IsNullOrEmpty(v1Recebido)) return false;
        var esperado = GerarHash(segredo, MontarManifesto(dataId, requestId, ts));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperado),
            Encoding.UTF8.GetBytes(v1Recebido));
    }
}
