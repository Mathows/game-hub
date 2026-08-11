using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Consentimento versionado.
///
/// USA <see cref="IDbContextFactory{TContext}"/>, e não o DbContext injetado direto, porque
/// este serviço é chamado pelo LAYOUT — ou seja, em toda página, concorrendo com a consulta
/// da própria página. Um DbContext Scoped não aceita duas operações simultâneas
/// ("A second operation was started on this context instance"). Criando e descartando um
/// contexto por operação, cada consulta fica isolada.
///
/// Regra prática: componente que consulta o banco no layout → fábrica, não contexto injetado.
/// </summary>
public class ConsentimentoService : IConsentimentoService
{
    private readonly IDbContextFactory<GameHubDbContext> _fabrica;

    public ConsentimentoService(IDbContextFactory<GameHubDbContext> fabrica) => _fabrica = fabrica;

    public async Task<TermoDeUso?> ObterVigenteAsync()
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.TermosDeUso.AsNoTracking().FirstOrDefaultAsync(t => t.Vigente);
    }

    public async Task<List<TermoDeUso>> ObterHistoricoAsync()
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.TermosDeUso.AsNoTracking()
            .OrderByDescending(t => t.PublicadoEm)
            .ToListAsync();
    }

    public async Task RegistrarAceiteAsync(
        string applicationUserId, int termoId, string? ip, string? userAgent)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        // IDEMPOTENTE: se a pessoa clicar duas vezes (ou recarregar a página do aceite),
        // não geramos duas provas do mesmo consentimento — a primeira já vale.
        // (O índice unique no banco garante isso mesmo em corrida entre requisições.)
        var jaAceitou = await db.AceitesTermo
            .AnyAsync(a => a.ApplicationUserId == applicationUserId && a.TermoDeUsoId == termoId);
        if (jaAceitou) return;

        db.AceitesTermo.Add(new AceiteTermo
        {
            ApplicationUserId = applicationUserId,
            TermoDeUsoId = termoId,
            AceitoEm = DateTime.Now,
            IpOrigem = ip,
            // O User-Agent pode ser enorme; a coluna tem limite. Cortar aqui evita
            // uma exceção do banco por causa de um dado que é só contexto da prova.
            UserAgent = userAgent?.Length > 400 ? userAgent[..400] : userAgent
        });

        await db.SaveChangesAsync();
    }

    public async Task<bool> PrecisaAceitarAsync(string applicationUserId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();

        var vigente = await db.TermosDeUso.AsNoTracking()
            .Where(t => t.Vigente)
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

        // Nenhum termo publicado: não há o que aceitar (não trava o sistema).
        if (vigente == 0) return false;

        // Um único predicado cobre os dois casos: quem NUNCA aceitou nada e quem aceitou
        // só uma versão anterior. "Não aceitou a vigente" é a pergunta certa.
        return !await db.AceitesTermo
            .AnyAsync(a => a.ApplicationUserId == applicationUserId && a.TermoDeUsoId == vigente);
    }

    public async Task<List<AceiteTermo>> AceitesDoUsuarioAsync(string applicationUserId)
    {
        await using var db = await _fabrica.CreateDbContextAsync();
        return await db.AceitesTermo.AsNoTracking()
            .Include(a => a.Termo)
            .Where(a => a.ApplicationUserId == applicationUserId)
            .OrderByDescending(a => a.AceitoEm)
            .ToListAsync();
    }
}
