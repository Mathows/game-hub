using Microsoft.AspNetCore.Identity;

namespace GameHub.Web.Data;

// Dados de perfil do usuário de login. Herda tudo do IdentityUser (Email, UserName, senha...)
// e acrescenta o nome da pessoa. [PersonalData] marca como DADO PESSOAL — o Identity já sabe
// incluir/remover esses campos nas telas de "baixar/excluir meus dados" (LGPD).
public class ApplicationUser : IdentityUser
{
    [PersonalData] public string? Nome { get; set; }
    [PersonalData] public string? Sobrenome { get; set; }
}
