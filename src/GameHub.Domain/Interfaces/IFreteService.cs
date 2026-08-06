namespace GameHub.Domain.Interfaces;

/// <summary>O frete calculado: valor, prazo e a descrição amigável.</summary>
public record ResultadoFrete(decimal Valor, int PrazoDias, string Descricao);

/// <summary>
/// Cálculo de frete — PROVIDER PLUGÁVEL (Sistema.md §4.1/Fase 10): a API real dos
/// Correios exige CONTRATO PAGO (o web service grátis foi descontinuado ~2023), então
/// hoje simulamos por tabela; amanhã pluga um agregador (Melhor Envio/Frenet) ou os
/// Correios de verdade — trocando só o registro na DI.
/// </summary>
public interface IFreteService
{
    /// <summary>Calcula o frete para um CEP de destino e uma quantidade de itens.</summary>
    Task<ResultadoFrete> CalcularAsync(string cepDestino, int quantidadeItens);
}
