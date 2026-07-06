using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Enums;
using GameHub.Domain.Services;
using GameHub.Infrastructure.Data;
using GameHub.Web.Data;
using GameHub.Web.Services;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Endpoints APENAS EDUCATIVOS (mapeados só em Development). Servem para "ver" dois conceitos:
/// 1) por que a tela mostrava Pendente mesmo com o banco Pago (cache do DbContext Scoped);
/// 2) os três tempos de vida de DI (Transient / Scoped / Singleton) na prática.
/// </summary>
public static class DemoEndpoints
{
    public static void MapDemoEndpoints(this WebApplication app)
    {
        // ───────────────────────────────────────────────────────────────────────────
        // DEMO 1 — GET /demo/cache/{pedidoId}
        // Reproduz o BUG (leitura com tracking = stale) e a CORREÇÃO (AsNoTracking = fresco)
        // lado a lado, numa chamada só. Não é destrutivo: desfaz a mudança no final.
        // ───────────────────────────────────────────────────────────────────────────
        app.MapGet("/demo/cache/{pedidoId:int}", async (
            int pedidoId,
            GameHubDbContext ctxPagina,           // este é o contexto Scoped "da página" (deste request)
            IServiceScopeFactory scopeFactory) =>
        {
            // 1) A "página" carrega o pedido → ele fica RASTREADO neste contexto.
            var pedido = await ctxPagina.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
            if (pedido is null) return Results.NotFound($"Pedido {pedidoId} não existe.");
            var statusInicial = pedido.Status;

            // 2) O "webhook" roda em OUTRO escopo (outro DbContext) e muda o status no banco.
            var statusNovo = statusInicial == StatusPedido.Pago ? StatusPedido.Pendente : StatusPedido.Pago;
            using (var escopoWebhook = scopeFactory.CreateScope())
            {
                var ctxWebhook = escopoWebhook.ServiceProvider.GetRequiredService<GameHubDbContext>();
                var p = await ctxWebhook.Pedidos.FirstAsync(x => x.Id == pedidoId);
                p.Status = statusNovo;
                await ctxWebhook.SaveChangesAsync();
            }

            // 3) A "página" relê no MESMO contexto, das duas formas:
            var comTracking = (await ctxPagina.Pedidos.FirstAsync(p => p.Id == pedidoId)).Status;                 // BUG: vem do cache
            var comAsNoTracking = (await ctxPagina.Pedidos.AsNoTracking().FirstAsync(p => p.Id == pedidoId)).Status; // FIX: fresco do banco

            // 4) Confirma o valor REAL no banco (escopo novo, sem cache).
            StatusPedido statusRealNoBanco;
            using (var escopoConfere = scopeFactory.CreateScope())
            {
                var ctx = escopoConfere.ServiceProvider.GetRequiredService<GameHubDbContext>();
                statusRealNoBanco = (await ctx.Pedidos.AsNoTracking().FirstAsync(x => x.Id == pedidoId)).Status;
            }

            // 5) Desfaz a mudança (demo não-destrutiva): volta ao status inicial.
            using (var escopoRevert = scopeFactory.CreateScope())
            {
                var ctx = escopoRevert.ServiceProvider.GetRequiredService<GameHubDbContext>();
                var p = await ctx.Pedidos.FirstAsync(x => x.Id == pedidoId);
                p.Status = statusInicial;
                await ctx.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                explicacao = "O contexto Scoped da pagina rastreou o pedido. Uma mudanca feita em OUTRO escopo (o webhook) NAO e vista por uma releitura COM tracking (identity map / cache de 1o nivel do EF). Com AsNoTracking, a releitura vem FRESCA do banco.",
                pedido = pedidoId,
                statusInicial = statusInicial.ToString(),
                mudancaExterna = $"{statusInicial} -> {statusNovo} (feita em outro escopo, como o webhook)",
                leitura_COM_tracking__BUG = comTracking.ToString(),
                leitura_COM_AsNoTracking__CORRIGIDO = comAsNoTracking.ToString(),
                statusRealNoBanco = statusRealNoBanco.ToString(),
                obs = "A mudanca foi revertida ao final (demo nao-destrutiva)."
            });
        });

        // ───────────────────────────────────────────────────────────────────────────
        // DEMO 2 — GET /demo/tempos-de-vida
        // Mostra os 3 tempos de vida pelo "hash" (identidade) das instâncias.
        // ───────────────────────────────────────────────────────────────────────────
        app.MapGet("/demo/tempos-de-vida", (HttpContext http) =>
        {
            var sp = http.RequestServices;   // container de serviços DESTE request

            // Transient: cada resolução cria um NOVO objeto → hashes diferentes.
            var t1 = sp.GetRequiredService<CalculadoraAluguel>().GetHashCode();
            var t2 = sp.GetRequiredService<CalculadoraAluguel>().GetHashCode();

            // Scoped: no MESMO request → mesma instância (mesmo hash).
            // Recarregue a página (novo request) e este número MUDA.
            var s1 = sp.GetRequiredService<CarrinhoService>().GetHashCode();
            var s2 = sp.GetRequiredService<CarrinhoService>().GetHashCode();

            // Singleton: sempre a MESMA instância — inclusive entre reloads.
            var g1 = sp.GetRequiredService<IEmailSender<ApplicationUser>>().GetHashCode();
            var g2 = sp.GetRequiredService<IEmailSender<ApplicationUser>>().GetHashCode();

            return Results.Ok(new
            {
                explicacao = "Compare os hashes. Transient: 2 numeros DIFERENTES (novo a cada uso). Scoped: 2 IGUAIS neste request, mas MUDA se recarregar (novo escopo). Singleton: sempre IGUAL, inclusive entre reloads.",
                transient_CalculadoraAluguel = new { instancia1 = t1, instancia2 = t2, iguais = t1 == t2 },
                scoped_CarrinhoService = new { instancia1 = s1, instancia2 = s2, iguais = s1 == s2, dica = "recarregue a pagina: este numero MUDA" },
                singleton_EmailSender = new { instancia1 = g1, instancia2 = g2, iguais = g1 == g2, dica = "recarregue a pagina: continua IGUAL" }
            });
        });
    }
}
