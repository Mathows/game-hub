using System.Security.Claims;
using GameHub.Domain.Interfaces;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Download do XML (documento fiscal oficial) e da DANFE (PDF) da nota.
///
/// É o "VisualizarNFe.ashx do FinFix" feito do jeito MODERNO: em vez de parâmetro
/// criptografado na URL, o endpoint exige LOGIN e valida que o pedido é DO USUÁRIO —
/// e o servidor busca o arquivo no emissor e repassa (proxy). O cliente nunca vê a
/// URL do provider, e ninguém baixa nota alheia trocando o id.
/// </summary>
public static class NotaFiscalEndpoints
{
    public static void MapNotaFiscalEndpoints(this WebApplication app)
    {
        app.MapGet("/nota/{pedidoId:int}/download/{tipo}", async (
            int pedidoId,
            string tipo,
            ClaimsPrincipal user,
            IEmissorNotaFiscal emissor,
            IHttpClientFactory httpFactory,
            IConfiguration config) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            // Valida o DONO: ObterPorPedidoAsync só devolve a nota se o pedido for do usuário.
            var nota = await emissor.ObterPorPedidoAsync(pedidoId, userId);
            if (nota is null)
                return Results.NotFound("Nota não encontrada.");

            var (url, contentType, extensao) = tipo.ToLowerInvariant() switch
            {
                "xml" => (nota.UrlXml, "application/xml", "xml"),
                "pdf" => (nota.UrlPdf, "application/pdf", "pdf"),
                _ => (null, "", "")
            };
            if (string.IsNullOrEmpty(url))
                return Results.NotFound("Arquivo não disponível para esta nota (emitida pelo provider simulado?).");

            // PROXY: o servidor busca no emissor e repassa os bytes ao cliente.
            // BUG APRENDIDO: os arquivos do PlugNotas TAMBÉM exigem o x-api-key (sem ele, 401) —
            // o token fica NO SERVIDOR; o cliente nunca o vê (mais um motivo pro proxy existir).
            var http = httpFactory.CreateClient();
            using var requisicao = new HttpRequestMessage(HttpMethod.Get, url);
            var token = config["NotaFiscal:PlugNotas:Token"];
            if (!string.IsNullOrEmpty(token))
                requisicao.Headers.Add("x-api-key", token);
            var resposta = await http.SendAsync(requisicao);
            if (!resposta.IsSuccessStatusCode)
                return Results.Problem("O emissor não devolveu o arquivo — tente novamente.");

            var bytes = await resposta.Content.ReadAsByteArrayAsync();
            var nomeArquivo = $"NFe-{nota.ChaveAcesso ?? nota.Numero.ToString("D9")}.{extensao}";
            return Results.File(bytes, contentType, nomeArquivo);
        })
        .RequireAuthorization();   // sem login, nem chega no código acima
    }
}
