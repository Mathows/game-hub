using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GameHub.Web.Data;

/// <summary>
/// "Fábrica de crachá" do usuário: monta o conjunto de claims (a identidade) no login.
/// Além dos claims padrão (id, e-mail...), acrescentamos um claim <c>"nome"</c> com o nome
/// de exibição da pessoa. Assim as telas leem o nome DIRETO do cookie (sem ir ao banco a
/// cada página) — ex.: o cabeçalho mostra "Matheus Alexandre" em vez do e-mail.
/// </summary>
// IMPORTANTE: herda da versão de 2 parâmetros (<ApplicationUser, IdentityRole>). É ela que
// inclui os CLAIMS DE ROLE (papel) na identidade — sem isso, o [Authorize(Roles="Admin")]
// não reconheceria o admin. A base já adiciona os roles; nós só somamos o claim "nome".
public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);   // claims padrão do Identity

        // Nome de exibição: "Nome Sobrenome" se houver; senão cai no e-mail (UserName).
        var nomeExibicao = string.IsNullOrWhiteSpace(user.Nome)
            ? (user.UserName ?? string.Empty)
            : $"{user.Nome} {user.Sobrenome}".Trim();

        identity.AddClaim(new Claim("nome", nomeExibicao));
        return identity;
    }
}
