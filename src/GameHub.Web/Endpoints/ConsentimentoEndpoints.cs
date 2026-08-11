using System.Security.Claims;
using GameHub.Domain.Interfaces;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Recebe o aceite dos termos vindo da faixa de re-aceite (LGPD · Fase 11).
///
/// É um POST tradicional de formulário, não uma chamada interativa do Blazor, porque o
/// aceite grava o IP como PROVA do consentimento — e só uma requisição HTTP real tem IP
/// (no circuito SignalR do Blazor, o HttpContext é null).
/// </summary>
public static class ConsentimentoEndpoints
{
    public static void MapConsentimentoEndpoints(this WebApplication app)
    {
        app.MapPost("/consentimento/aceitar", async (
            HttpContext context,
            IConsentimentoService consentimento,
            [Microsoft.AspNetCore.Mvc.FromForm] int termoId) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId is null) return Results.Unauthorized();

            // Confere que o termo recebido é REALMENTE o vigente. Nunca confie no id que
            // veio do formulário: alguém poderia postar o id de uma versão antiga e ficar
            // "em conformidade" tendo aceitado outra coisa.
            var vigente = await consentimento.ObterVigenteAsync();
            if (vigente is null || vigente.Id != termoId)
                return Results.BadRequest("Termo inválido ou não está em vigor.");

            await consentimento.RegistrarAceiteAsync(
                userId,
                vigente.Id,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString());

            // Volta para onde a pessoa estava (a faixa aparece em qualquer página).
            var voltarPara = context.Request.Headers.Referer.ToString();
            return Results.LocalRedirect(
                string.IsNullOrWhiteSpace(voltarPara) ? "/" : NormalizarLocal(voltarPara));
        })
        .RequireAuthorization();   // só quem está logado pode registrar um aceite

        // LocalRedirect exige caminho local ("/algo"): o Referer é uma URL absoluta.
        // Extrair só o caminho evita open redirect (mandar o usuário para fora do site).
        static string NormalizarLocal(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.PathAndQuery : "/";
    }
}
