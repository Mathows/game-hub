using System.Text.Json.Serialization;

namespace GameHub.Web.Services;

/// <summary>
/// Verificação anti-robô (reCAPTCHA v3). O v3 é INVISÍVEL: um script do Google observa o
/// comportamento na página e dá uma NOTA de 0.0 (robô) a 1.0 (humano). No submit, o JS gera
/// um token; o servidor manda o token ao Google (siteverify) e decide pelo score.
/// Fica na camada Web (é uma preocupação de formulário/HTTP, não regra de negócio).
/// </summary>
public interface IRecaptchaService
{
    Task<bool> VerificarAsync(string? token);
}

/// <summary>
/// Implementação real: chama https://www.google.com/recaptcha/api/siteverify com a
/// SECRET KEY (user-secrets) + o token do formulário, e aceita score >= 0.5.
/// </summary>
public class RecaptchaGoogleService : IRecaptchaService
{
    private const double NotaMinima = 0.5;   // corte: abaixo disso tratamos como robô

    private readonly HttpClient _http;
    private readonly string _secretKey;
    private readonly ILogger<RecaptchaGoogleService> _log;

    public RecaptchaGoogleService(HttpClient http, string secretKey, ILogger<RecaptchaGoogleService> log)
    {
        _http = http;
        _secretKey = secretKey;
        _log = log;
    }

    public async Task<bool> VerificarAsync(string? token)
    {
        // Sem token = o JS não rodou (ou é um bot que nem executa JS) → reprova.
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            // O siteverify recebe um POST de formulário (não JSON).
            using var conteudo = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _secretKey,
                ["response"] = token
            });
            var resposta = await _http.PostAsync("recaptcha/api/siteverify", conteudo);
            var dto = await resposta.Content.ReadFromJsonAsync<RespostaSiteverify>();

            var aprovado = dto is { Success: true } && dto.Score >= NotaMinima;
            _log.LogInformation("reCAPTCHA: success={Success} score={Score} → {Resultado}",
                dto?.Success, dto?.Score, aprovado ? "aprovado" : "reprovado");
            return aprovado;
        }
        catch (Exception ex)
        {
            // Se o Google estiver fora do ar, não bloqueamos o cadastro (decisão de
            // disponibilidade > proteção; num banco seria o contrário).
            _log.LogWarning(ex, "reCAPTCHA: falha ao verificar — deixando passar (fail-open).");
            return true;
        }
    }

    // Formato da resposta do siteverify (só o que usamos).
    private sealed class RespostaSiteverify
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("score")] public double Score { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
    }
}

/// <summary>Sem chaves configuradas (dev), o reCAPTCHA fica DESLIGADO: sempre aprova.</summary>
public class RecaptchaDesativadoService : IRecaptchaService
{
    public Task<bool> VerificarAsync(string? token) => Task.FromResult(true);
}
