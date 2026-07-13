using System.Security.Claims;
using GameHub.Domain.Interfaces;

namespace GameHub.Web.Services;

/// <summary>
/// Implementação de <see cref="IUsuarioAtual"/> para a Web: lê o Id do usuário logado do
/// HttpContext (o claim NameIdentifier). É a "ponte" que deixa o interceptor de auditoria
/// (em Infrastructure) saber QUEM está logado sem depender de HTTP diretamente.
/// </summary>
public class UsuarioAtual : IUsuarioAtual
{
    private readonly IHttpContextAccessor _http;

    public UsuarioAtual(IHttpContextAccessor http) => _http = http;

    public string? Id => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
