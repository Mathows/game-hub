using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório de jogos usando o Entity Framework Core.
/// Recebe o GameHubDbContext por Injeção de Dependência (no construtor).
/// </summary>
public class JogoRepository : IJogoRepository
{
    private readonly GameHubDbContext _context;

    // O contexto chega PRONTO aqui — não damos "new". Quem monta é o container de DI.
    public JogoRepository(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task<List<Jogo>> ObterTodosAsync(int? plataformaId = null, string? busca = null)
    {
        var agora = DateTime.Now;
        // Include = também traz os dados relacionados (plataforma e gênero) numa só consulta.
        // FILTERED INCLUDE: das promoções, só as que valem AGORA (não carrega histórico à toa).
        var query = _context.Jogos
            .Include(j => j.Plataforma)
            .Include(j => j.Genero)
            .Include(j => j.Promocoes.Where(p => p.Ativa && p.Inicio <= agora && agora <= p.Fim))
            .AsQueryable();

        if (plataformaId.HasValue)
            query = query.Where(j => j.PlataformaId == plataformaId.Value);

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(j => j.Titulo.Contains(busca));

        return await query.OrderBy(j => j.Titulo).ToListAsync();
    }

    public async Task<List<Jogo>> ObterDestaquesAsync(int quantidade = 4)
    {
        var agora = DateTime.Now;
        return await _context.Jogos
            .Include(j => j.Plataforma)
            .Include(j => j.Promocoes.Where(p => p.Ativa && p.Inicio <= agora && agora <= p.Fim))
            .OrderByDescending(j => j.DataCadastro)
            .Take(quantidade)
            .ToListAsync();
    }

    public async Task<Jogo?> ObterPorIdAsync(int id)
    {
        var agora = DateTime.Now;
        return await _context.Jogos
            .Include(j => j.Plataforma)
            .Include(j => j.Genero)
            .Include(j => j.Promocoes.Where(p => p.Ativa && p.Inicio <= agora && agora <= p.Fim))
            .FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task AdicionarAsync(Jogo jogo)
    {
        _context.Jogos.Add(jogo);
        await _context.SaveChangesAsync();   // aqui o SQL INSERT realmente acontece
    }

    public async Task AtualizarAsync(Jogo jogo)
    {
        _context.Jogos.Update(jogo);
        await _context.SaveChangesAsync();
    }

    public async Task RemoverAsync(int id)
    {
        var jogo = await _context.Jogos.FindAsync(id);
        if (jogo is not null)
        {
            _context.Jogos.Remove(jogo);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Plataforma>> ObterPlataformasAsync()
        => await _context.Plataformas.OrderBy(p => p.Nome).ToListAsync();

    public async Task<List<Genero>> ObterGenerosAsync()
        => await _context.Generos.OrderBy(g => g.Nome).ToListAsync();
}
