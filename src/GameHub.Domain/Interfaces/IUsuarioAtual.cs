namespace GameHub.Domain.Interfaces;

/// <summary>
/// Abstrai "quem é o usuário logado agora" (só o Id). A implementação real vive na camada Web
/// (lê o HttpContext), mas o Domain/Infrastructure dependem só desta interface — assim o
/// Infrastructure NÃO precisa conhecer HTTP (mantém as camadas desacopladas).
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>Id do usuário logado (claim NameIdentifier), ou null se anônimo/sistema.</summary>
    string? Id { get; }
}
