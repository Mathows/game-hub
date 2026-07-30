using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Provider SIMULADO (grátis, sem validade fiscal): faz o papel do emissor/SEFAZ.
/// Comporta-se como o real até na REJEIÇÃO: NF-e de verdade exige CPF/CNPJ do
/// destinatário — sem ele, rejeita com motivo (o cliente corrige no perfil e reemite).
/// A chave de 44 dígitos segue a ESTRUTURA real: UF(2) AAMM(4) CNPJ(14) mod(2)
/// série(3) número(9) tpEmis(1) cNF(8) DV(1) — com dados fictícios.
/// </summary>
public class NotaFiscalProviderSimulado : INotaFiscalProvider
{
    private const string CnpjEmitenteFicticio = "00000000000191";

    public Task<ResultadoEmissao> EmitirAsync(NotaFiscal nota, Pedido pedido)
    {
        // Regra fiscal real: nota precisa do CPF/CNPJ do destinatário.
        var cpfCnpj = pedido.Cliente?.CpfCnpj;
        if (string.IsNullOrWhiteSpace(cpfCnpj))
        {
            return Task.FromResult(new ResultadoEmissao(
                Autorizada: false,
                ChaveAcesso: null,
                Protocolo: null,
                MotivoRejeicao: "Rejeição 999 (simulada): destinatário sem CPF/CNPJ. " +
                                "Informe seus dados fiscais no perfil e reemita."));
        }

        var agora = DateTime.Now;
        var chave =
            "35" +                                  // UF: SP
            agora.ToString("yyMM") +                // ano/mês da emissão
            CnpjEmitenteFicticio +                  // CNPJ do emitente (fictício)
            "55" +                                  // modelo 55 = NF-e
            nota.Serie.ToString("D3") +             // série
            nota.Numero.ToString("D9") +            // número REAL da nota (sequencial nosso)
            "1" +                                   // tipo de emissão
            pedido.Id.ToString("D8") +              // código numérico (determinístico)
            ((nota.Numero + pedido.Id) % 10);       // dígito verificador (fake)

        return Task.FromResult(new ResultadoEmissao(
            Autorizada: true,
            ChaveAcesso: chave,                     // 44 dígitos
            Protocolo: $"SIM-{agora:yyyyMMddHHmmss}-{nota.Numero}",
            MotivoRejeicao: null));
    }
}
