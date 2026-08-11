using System.Security.Claims;
using GameHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace GameHub.Web.Autorizacao;

/// <summary>
/// A EXIGÊNCIA: "a idade do usuário precisa alcançar a classificação indicativa do jogo".
///
/// Um requirement é só um marcador — ele descreve O QUE se exige, sem dizer como verificar.
/// Quem verifica é o handler. Essa separação permite ter várias formas de satisfazer a
/// mesma exigência (ex.: idade própria OU autorização de responsável, no futuro) sem
/// tocar em quem usa a policy.
/// </summary>
public class ClassificacaoIndicativaRequirement : IAuthorizationRequirement;

/// <summary>
/// Decide se ESTE usuário pode comprar ESTE jogo.
///
/// Note o <c>AuthorizationHandler&lt;Requirement, Jogo&gt;</c>: dois tipos genéricos. É a
/// autorização BASEADA EM RECURSO — a decisão não depende só de quem pede, mas do objeto
/// pedido. É a diferença essencial em relação a role: role responde "quem é você?",
/// isto responde "você pode fazer isso COM ESTA COISA?".
/// </summary>
public class ClassificacaoIndicativaHandler
    : AuthorizationHandler<ClassificacaoIndicativaRequirement, Jogo>
{
    /// <summary>Nome do claim onde guardamos a data de nascimento no cookie.</summary>
    public const string ClaimDataNascimento = "data_nascimento";

    private readonly ILogger<ClassificacaoIndicativaHandler> _log;

    public ClassificacaoIndicativaHandler(ILogger<ClassificacaoIndicativaHandler> log) => _log = log;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClassificacaoIndicativaRequirement requirement,
        Jogo jogo)
    {
        var idadeMinima = (int)jogo.Classificacao;

        // Jogo livre: ninguém precisa provar idade. Sai cedo — a regra mais barata primeiro.
        if (idadeMinima == 0)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // A partir daqui é preciso saber a idade. Visitante anônimo não tem como provar.
        var textoNascimento = context.User.FindFirst(ClaimDataNascimento)?.Value;
        if (!DateOnly.TryParse(textoNascimento, out var nascimento))
        {
            // NÃO chamamos Fail(): apenas não damos Succeed().
            //
            // A diferença importa: Fail() é um VETO — ele derruba a autorização mesmo que
            // outro handler aprove. Aqui o certo é só "eu não sei dizer que sim", deixando
            // a porta aberta para um futuro handler (ex.: consentimento de responsável)
            // conseguir aprovar. Veto se reserva para violação real, não para falta de dado.
            return Task.CompletedTask;
        }

        if (CalcularIdade(nascimento, DateOnly.FromDateTime(DateTime.Today)) >= idadeMinima)
        {
            context.Succeed(requirement);
        }
        else
        {
            _log.LogInformation(
                "Classificação indicativa: acesso negado ao jogo {Jogo} ({Classificacao}+).",
                jogo.Titulo, idadeMinima);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Idade em anos completos.
    ///
    /// A subtração de anos não basta: quem nasceu em 31/12/2008 não tem 18 anos em
    /// 01/01/2026. Só completa a idade quando a data do aniversário CHEGA no ano corrente.
    /// </summary>
    public static int CalcularIdade(DateOnly nascimento, DateOnly hoje)
    {
        var idade = hoje.Year - nascimento.Year;
        if (nascimento.AddYears(idade) > hoje) idade--;   // ainda não fez aniversário este ano
        return idade;
    }
}
