using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="ICepService"/> usando o <b>ViaCEP</b> (https://viacep.com.br) —
/// API pública e <b>gratuita</b>. Recebe um <c>HttpClient</c> "tipado" (com a BaseAddress do
/// ViaCEP já configurada na DI, em Program.cs).
/// </summary>
public class ViaCepService : ICepService
{
    private readonly HttpClient _http;

    public ViaCepService(HttpClient http) => _http = http;

    public async Task<EnderecoCep?> BuscarAsync(string cep, CancellationToken ct = default)
    {
        // Deixa só os dígitos e valida: CEP brasileiro tem 8 dígitos.
        var digitos = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 8) return null;

        ViaCepResposta? dto;
        try
        {
            // ViaCEP: GET https://viacep.com.br/ws/{cep}/json/
            dto = await _http.GetFromJsonAsync<ViaCepResposta>($"ws/{digitos}/json/", ct);
        }
        catch
        {
            // Rede/JSON falhou → tratamos como "não encontrado" (a tela decide a mensagem).
            return null;
        }

        // CEP inexistente: o ViaCEP devolve {"erro":"true"}.
        // ATENÇÃO: o "erro" vem como STRING "true", não como booleano — por isso o campo é string.
        if (dto is null || dto.Erro == "true") return null;

        return new EnderecoCep(
            Cep: digitos,
            Logradouro: dto.Logradouro ?? string.Empty,
            Bairro: dto.Bairro ?? string.Empty,
            Cidade: dto.Localidade ?? string.Empty,   // ViaCEP chama a cidade de "localidade"
            Uf: dto.Uf ?? string.Empty);
    }

    // Formato do JSON do ViaCEP (só os campos que usamos).
    private sealed class ViaCepResposta
    {
        [JsonPropertyName("logradouro")] public string? Logradouro { get; set; }
        [JsonPropertyName("bairro")] public string? Bairro { get; set; }
        [JsonPropertyName("localidade")] public string? Localidade { get; set; }
        [JsonPropertyName("uf")] public string? Uf { get; set; }
        [JsonPropertyName("erro")] public string? Erro { get; set; }
    }
}
