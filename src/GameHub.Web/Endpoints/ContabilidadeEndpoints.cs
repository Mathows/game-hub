using GameHub.Domain.Interfaces;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Download do pacote mensal da contabilidade — SÓ ADMIN (RequireRole).
/// O ZIP é gerado em memória na hora e devolvido direto (sem arquivo temporário).
/// </summary>
public static class ContabilidadeEndpoints
{
    public static void MapContabilidadeEndpoints(this WebApplication app)
    {
        app.MapGet("/admin/contabilidade/pacote/{ano:int}/{mes:int}", async (
            int ano, int mes, IContabilidadeService contabilidade) =>
        {
            if (mes is < 1 or > 12 || ano is < 2020 or > 2100)
                return Results.BadRequest("Período inválido.");

            var pacote = await contabilidade.GerarPacoteAsync(ano, mes);
            return pacote is null
                ? Results.NotFound("Não há notas no período informado.")
                : Results.File(pacote.Zip, "application/zip", pacote.NomeArquivo);
        })
        // Policy nomeada em vez de RequireRole solto: a regra de "quem lida com fiscal"
        // fica num lugar só (Autorizacao/Policies.cs), igual à tela de contabilidade.
        .RequireAuthorization(GameHub.Web.Autorizacao.Policies.PodeEmitirFiscal);
    }
}
