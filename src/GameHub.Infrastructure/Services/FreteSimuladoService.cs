using GameHub.Domain.Interfaces;
using GameHub.Domain.Services;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Frete SIMULADO por tabela de região — usando uma regra REAL do CEP brasileiro:
/// o PRIMEIRO DÍGITO define a região postal (0=SP capital ... 9=RS). A loja "fica"
/// em São José dos Campos/SP (região 1), então quanto mais longe a região, mais caro
/// e mais demorado. Determinístico e explicável (nada de random).
/// </summary>
public class FreteSimuladoService : IFreteService
{
    private const decimal ValorBase = 12.90m;
    private const decimal PorItem = 2.50m;

    // (fator de distância, dias extras, nome da região) indexado pelo 1º dígito do CEP.
    private static readonly (decimal Fator, int DiasExtras, string Regiao)[] Tabela =
    {
        (1.0m, 1, "Grande São Paulo"),          // 0
        (0.8m, 0, "Interior de SP"),            // 1 ← a loja é daqui (SJC)
        (1.2m, 2, "RJ / ES"),                   // 2
        (1.3m, 2, "MG"),                        // 3
        (1.6m, 4, "BA / SE"),                   // 4
        (1.8m, 5, "PE / AL / PB / RN"),         // 5
        (1.9m, 6, "CE / PI / MA / PA / AM..."), // 6
        (1.5m, 4, "DF / GO / TO / MT / MS..."), // 7
        (1.3m, 3, "PR / SC"),                   // 8
        (1.4m, 3, "RS")                         // 9
    };

    public Task<ResultadoFrete> CalcularAsync(string cepDestino, int quantidadeItens)
    {
        var digitos = ValidadorCpfCnpj.SoDigitos(cepDestino);   // reusa o "só dígitos"
        if (digitos.Length != 8)
            throw new InvalidOperationException("CEP de destino inválido para o cálculo do frete.");
        if (quantidadeItens < 1) quantidadeItens = 1;

        var regiao = Tabela[digitos[0] - '0'];
        var valor = Math.Round(ValorBase * regiao.Fator + PorItem * quantidadeItens, 2);
        var prazo = 2 + regiao.DiasExtras;                       // 2 dias de base + distância

        return Task.FromResult(new ResultadoFrete(
            valor, prazo, $"Envio padrão para {regiao.Regiao} — até {prazo} dias úteis"));
    }
}
