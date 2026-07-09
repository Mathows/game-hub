using System.Text.Json;
using System.Text.Json.Serialization;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Payments;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Endpoint que RECEBE a notificação do gateway (o webhook). Aqui é o "avesso" do HTTP normal:
/// não somos nós que perguntamos — o Mercado Pago é quem chama esta URL quando um pagamento muda.
/// </summary>
public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/webhooks/pagamento", async (
            HttpRequest request,
            IPagamentoService pagamentos,
            IConfiguration config,
            ILoggerFactory loggerFactory) =>
        {
            var log = loggerFactory.CreateLogger("WebhookPagamento");
            var segredo = config["MercadoPago:WebhookSecret"] ?? string.Empty;

            // 1) Cabeçalhos que o Mercado Pago envia
            var xSignature = request.Headers["x-signature"].ToString();  // "ts=...,v1=..."
            var xRequestId = request.Headers["x-request-id"].ToString();
            var dataId = request.Query["data.id"].ToString();            // id do pagamento (na query)

            // Quebra o x-signature em ts e v1
            string ts = "", v1 = "";
            foreach (var parte in xSignature.Split(','))
            {
                var kv = parte.Split('=', 2);
                if (kv.Length != 2) continue;
                var chave = kv[0].Trim();
                if (chave == "ts") ts = kv[1].Trim();
                else if (chave == "v1") v1 = kv[1].Trim();
            }

            // 2) VALIDAR A ASSINATURA — se não bater, a chamada não é confiável: recusa.
            if (!MercadoPagoAssinatura.Validar(segredo, dataId, xRequestId, ts, v1))
            {
                log.LogWarning("Webhook recusado: assinatura inválida (data.id={DataId}).", dataId);
                return Results.Unauthorized();
            }

            // 3) Ler o corpo. (No MP real, o corpo traz só o id do pagamento e a gente
            //    chamaria a API do MP para saber o status e o external_reference. Na nossa
            //    SIMULAÇÃO, já mandamos pedidoId e status no corpo para simplificar.)
            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = await JsonSerializer.DeserializeAsync<WebhookPayload>(request.Body, opcoes);

            if (payload?.Data is { Status: "approved", PedidoId: > 0 } dados)
            {
                var ok = await pagamentos.ConfirmarPagamentoAsync(dados.PedidoId);
                log.LogInformation("Pagamento do pedido {Pedido}: {Resultado}.",
                    dados.PedidoId, ok ? "confirmado (Pago)" : "pedido não encontrado");
            }

            // 4) Responder 200 sempre que processamos: assim o MP não fica reenviando.
            return Results.Ok();
        });
    }

    // Formato do corpo que tratamos (simulação).
    private sealed class WebhookPayload
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("data")] public DadosPagamento? Data { get; set; }
    }

    private sealed class DadosPagamento
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("pedidoId")] public int PedidoId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }
}
