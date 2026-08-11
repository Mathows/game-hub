using Microsoft.AspNetCore.Identity;

namespace GameHub.Web.Data;

// Dados de perfil do usuário de login. Herda tudo do IdentityUser (Email, UserName, senha...)
// e acrescenta o nome da pessoa. [PersonalData] marca como DADO PESSOAL — o Identity já sabe
// incluir/remover esses campos nas telas de "baixar/excluir meus dados" (LGPD).
public class ApplicationUser : IdentityUser
{
    [PersonalData] public string? Nome { get; set; }
    [PersonalData] public string? Sobrenome { get; set; }

    /// <summary>
    /// Data de nascimento — atributo da PESSOA, usado pela policy de classificação
    /// indicativa (Fase 11 · P4). Guardamos a DATA, nunca a idade: idade calculada e
    /// gravada fica errada no dia seguinte ao aniversário.
    ///
    /// [PersonalData] entra na exportação/exclusão do titular (LGPD).
    /// </summary>
    [PersonalData] public DateOnly? DataNascimento { get; set; }
}
