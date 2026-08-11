using GameHub.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.Web.Endpoints;

/// <summary>
/// Salva a data de nascimento do usuário (usada pela policy de classificação indicativa).
///
/// POR QUE UM ENDPOINT, e não um botão interativo na página:
/// a data de nascimento vira um CLAIM no cookie de autenticação. Claim é um SNAPSHOT
/// tirado no login — gravar a data no banco NÃO muda o crachá que a pessoa já está
/// carregando. Sem renovar o cookie, ela salvaria a data e continuaria bloqueada até
/// sair e entrar de novo (um "bug" que confundiria qualquer usuário).
///
/// RefreshSignInAsync regera o cookie com os claims atualizados — e isso exige escrever
/// um cookie, ou seja, uma resposta HTTP real. No circuito SignalR do Blazor não há
/// resposta HTTP para carregar cookie, então tem de ser um POST tradicional.
/// </summary>
public static class PerfilEndpoints
{
    public static void MapPerfilEndpoints(this WebApplication app)
    {
        app.MapPost("/minha-conta/nascimento", async (
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            [FromForm] string? dataNascimento) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.Unauthorized();

            // NOTA sobre os redirects abaixo: o valor precisa ser "true"/"false", não "1"/"0".
            // O [SupplyParameterFromQuery] do Blazor converte a query string de forma ESTRITA
            // (mesma pegadinha do @bind de <select> para bool, que rendeu "True"/"False").
            if (!DateOnly.TryParse(dataNascimento, out var data))
                return Results.LocalRedirect("/minha-conta/dados?erroNascimento=true");

            // Validações de sanidade: data futura ou idade absurda são erro de digitação,
            // não dado válido. Rejeitar aqui evita "nasceu em 2225" no banco.
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            if (data > hoje || data < hoje.AddYears(-120))
                return Results.LocalRedirect("/minha-conta/dados?erroNascimento=true");

            user.DataNascimento = data;
            var resultado = await userManager.UpdateAsync(user);
            if (!resultado.Succeeded)
                return Results.LocalRedirect("/minha-conta/dados?erroNascimento=true");

            // O PASSO QUE FALTARIA: renova o cookie para o claim novo valer AGORA.
            await signInManager.RefreshSignInAsync(user);

            return Results.LocalRedirect("/minha-conta/dados?nascimentoSalvo=true");
        })
        // Só logado — e a proteção CSRF fica LIGADA: endpoints com [FromForm] são validados
        // automaticamente pelo middleware UseAntiforgery (o form envia <AntiforgeryToken />).
        // Nunca chamar DisableAntiforgery aqui: sem o token, outro site conseguiria postar
        // uma data de nascimento na conta de quem estivesse logado.
        .RequireAuthorization();
    }
}
