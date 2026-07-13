namespace GameHub.Domain.Interfaces;

/// <summary>Dados de endereço que a busca por CEP devolve.</summary>
public record EnderecoCep(string Cep, string Logradouro, string Bairro, string Cidade, string Uf);

/// <summary>
/// Busca de endereço por CEP. É uma abstração: hoje a implementação usa o ViaCEP (grátis),
/// mas trocar por outro provedor (BrasilAPI, Correios...) é só registrar outra classe na DI —
/// as telas continuam iguais. (Ver Sistema.md — provider plugável.)
/// </summary>
public interface ICepService
{
    /// <summary>Retorna o endereço do CEP, ou <c>null</c> se o CEP for inválido/não encontrado.</summary>
    Task<EnderecoCep?> BuscarAsync(string cep, CancellationToken ct = default);
}
