namespace GameHub.Domain.Interfaces;

/// <summary>O frete calculado: valor, prazo e a descrição amigável.</summary>
public record ResultadoFrete(decimal Valor, int PrazoDias, string Descricao);

/// <summary>
/// Cálculo de frete — PROVIDER PLUGÁVEL (Sistema.md §4.1/Fase 10): a API real dos
/// Correios exige CONTRATO PAGO (o web service grátis foi descontinuado ~2023), então
/// hoje calculamos por tabela própria (valores no banco, editáveis pelo admin); amanhã
/// pluga um agregador (Melhor Envio/Frenet) ou os Correios — trocando só o registro na DI.
/// </summary>
public interface IFreteService
{
    /// <summary>
    /// Calcula o frete para um CEP de destino.
    /// </summary>
    /// <param name="subtotalCompras">Subtotal dos produtos — usado na regra de frete grátis.</param>
    Task<ResultadoFrete> CalcularAsync(string cepDestino, int quantidadeItens, decimal subtotalCompras = 0);
}
