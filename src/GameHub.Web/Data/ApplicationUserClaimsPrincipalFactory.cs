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

        // Data de nascimento no CRACHÁ (claim), não consultada no banco a cada verificação.
        // A policy de classificação indicativa roda em toda listagem e em todo detalhe de
        // jogo: uma ida ao banco por jogo por página seria caro. O claim vive no cookie e
        // é renovado no login — e a data de nascimento é um dado que não muda.
        if (user.DataNascimento is DateOnly nascimento)
        {
            identity.AddClaim(new Claim(
                Autorizacao.ClassificacaoIndicativaHandler.ClaimDataNascimento,
                nascimento.ToString("yyyy-MM-dd")));
        }

        return identity;
    }
}
