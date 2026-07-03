using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Contrato do repositório de jogos: define O QUE dá para fazer com jogos,
/// sem dizer COMO (isso fica na implementação, na camada Infrastructure).
/// Quem usa (as telas) depende só desta interface — princípio da Injeção de Dependência.
/// </summary>
public interface IJogoRepository
{
    Task<List<Jogo>> ObterTodosAsync(int? plataformaId = null, string? busca = null);
    Task<List<Jogo>> ObterDestaquesAsync(int quantidade = 4);
    Task<Jogo?> ObterPorIdAsync(int id);
    Task AdicionarAsync(Jogo jogo);
    Task AtualizarAsync(Jogo jogo);
    Task RemoverAsync(int id);

    // Listas de apoio (para os filtros e formulários)
    Task<List<Plataforma>> ObterPlataformasAsync();
    Task<List<Genero>> ObterGenerosAsync();
}
