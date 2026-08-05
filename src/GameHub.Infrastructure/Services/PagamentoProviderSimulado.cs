using System.Text;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Provider de cobrança SIMULADO — gera artefatos NO FORMATO real (didática), sem valor:
/// - PIX: payload "copia e cola" na estrutura BR Code/EMV (id+valor+merchant+CRC fake);
/// - Boleto: linha digitável de 47 dígitos (banco/moeda/fator de vencimento/valor em centavos);
/// - Cartão: sem artefato — só a instrução (o gateway resolveria).
/// Na Fase de produção, o sandbox do Mercado Pago devolveria esses dados de verdade.
/// </summary>
public class PagamentoProviderSimulado : IPagamentoProvider
{
    public Task<DadosCobranca> GerarCobrancaAsync(Pedido pedido, FormaPagamento forma)
    {
        DadosCobranca dados = forma switch
        {
            FormaPagamento.Pix => new DadosCobranca(
                PixCopiaECola: GerarPayloadPix(pedido),
                BoletoLinhaDigitavel: null,
                BoletoVencimento: null,
                Instrucao: "Copie o código PIX e pague no app do seu banco. A confirmação chega pelo webhook."),

            FormaPagamento.Boleto => new DadosCobranca(
                PixCopiaECola: null,
                BoletoLinhaDigitavel: GerarLinhaDigitavel(pedido),
                BoletoVencimento: DateTime.Today.AddDays(3),          // vence em 3 dias
                Instrucao: "Pague a linha digitável até o vencimento. A baixa chega pela conciliação."),

            _ => new DadosCobranca(null, null, null,
                "Pagamento em processamento no gateway de cartão — a aprovação chega pelo webhook.")
        };
        return Task.FromResult(dados);
    }

    // PIX "copia e cola" na ESTRUTURA do BR Code (EMV TLV): campos id+tamanho+valor.
    // Dados fictícios (chave, cidade, CRC) — mas o formato é o que o app do banco lê.
    private static string GerarPayloadPix(Pedido pedido)
    {
        var valor = pedido.ValorTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var chavePix = $"gamehub-{pedido.Id:D6}@pix.sim";              // "chave" fictícia
        var sb = new StringBuilder();
        sb.Append("000201");                                            // payload format
        sb.Append($"26{(22 + chavePix.Length):D2}");                    // merchant account info
        sb.Append("0014BR.GOV.BCB.PIX");
        sb.Append($"01{chavePix.Length:D2}{chavePix}");
        sb.Append("52040000");                                          // categoria
        sb.Append("5303986");                                           // moeda 986 = BRL
        sb.Append($"54{valor.Length:D2}{valor}");                       // valor
        sb.Append("5802BR");
        sb.Append("5907GAMEHUB");                                       // nome do recebedor
        sb.Append("6009SAO PAULO");
        sb.Append($"62{("05" + $"{pedido.Id:D8}".Length.ToString("D2") + pedido.Id.ToString("D8")).Length:D2}");
        sb.Append($"05{pedido.Id.ToString("D8").Length:D2}{pedido.Id:D8}");   // txid = pedido
        sb.Append($"6304{(pedido.Id * 7919 % 65536):X4}");              // CRC16 fake
        return sb.ToString();
    }

    // Linha digitável de boleto (47 dígitos, 5 campos) — estrutura real, DVs fictícios.
    private static string GerarLinhaDigitavel(Pedido pedido)
    {
        var centavos = (long)(pedido.ValorTotal * 100);
        // Fator de vencimento: dias desde 07/10/1997 (regra FEBRABAN) para hoje+3.
        var fator = (DateTime.Today.AddDays(3) - new DateTime(1997, 10, 7)).Days % 10000;
        var nossoNumero = pedido.Id.ToString("D11");                    // o "NossoNumero" do FinFix!

        var campo1 = $"00190.0000{pedido.Id % 10}";
        var campo2 = $"{nossoNumero[..5]}.{nossoNumero[5..10]}{(pedido.Id + 1) % 10}";
        var campo3 = $"{nossoNumero[10..]}0000.00000{(pedido.Id + 2) % 10}";
        var dvGeral = (pedido.Id + 3) % 10;
        var campo5 = $"{fator:D4}{centavos:D10}";
        return $"{campo1} {campo2} {campo3} {dvGeral} {campo5}";
    }
}
