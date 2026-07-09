using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>Implementação das trocas com EF Core (Scoped, usa o GameHubDbContext).</summary>
public class TrocaService : ITrocaService
{
    private readonly GameHubDbContext _context;

    public TrocaService(GameHubDbContext context) => _context = context;

    public async Task<Troca> PropoAsync(string ofertanteUserId, string nome, int jogoOferecidoId, int jogoDesejadoId)
    {
        if (jogoOferecidoId == jogoDesejadoId)
            throw new InvalidOperationException("O jogo oferecido e o desejado precisam ser diferentes.");

        var cliente = await ObterOuCriarClienteAsync(ofertanteUserId, nome);

        var troca = new Troca
        {
            ClienteOfertante = cliente,
            JogoOferecidoId = jogoOferecidoId,
            JogoDesejadoId = jogoDesejadoId,
            Status = StatusTroca.Proposta,
            DataProposta = DateTime.Now
        };
        _context.Trocas.Add(troca);
        await _context.SaveChangesAsync();
        return troca;
    }

    public async Task<List<Troca>> ObterAbertasAsync(string userId)
        => await _context.Trocas
            .AsNoTracking()
            .Include(t => t.ClienteOfertante)
            .Include(t => t.JogoOferecido)
            .Include(t => t.JogoDesejado)
            .Where(t => t.Status == StatusTroca.Proposta
                     && t.ClienteOfertante!.ApplicationUserId != userId)   // só as dos OUTROS
            .OrderByDescending(t => t.DataProposta)
            .ToListAsync();

    public async Task<List<Troca>> ObterMinhasAsync(string userId)
        => await _context.Trocas
            .AsNoTracking()
            .Include(t => t.ClienteOfertante)
            .Include(t => t.ClienteReceptor)
            .Include(t => t.JogoOferecido)
            .Include(t => t.JogoDesejado)
            .Where(t => t.ClienteOfertante!.ApplicationUserId == userId
                     || (t.ClienteReceptor != null && t.ClienteReceptor.ApplicationUserId == userId))
            .OrderByDescending(t => t.DataProposta)
            .ToListAsync();

    public async Task<bool> AceitarAsync(int trocaId, string userId, string nome)
    {
        var troca = await _context.Trocas
            .Include(t => t.ClienteOfertante)
            .FirstOrDefaultAsync(t => t.Id == trocaId);

        if (troca is null || troca.Status != StatusTroca.Proposta)
            return false;
        if (troca.ClienteOfertante!.ApplicationUserId == userId)   // não posso aceitar a MINHA própria
            return false;

        var receptor = await ObterOuCriarClienteAsync(userId, nome);
        troca.ClienteReceptor = receptor;
        troca.Status = StatusTroca.Aceita;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecusarAsync(int trocaId, string userId)
    {
        var troca = await _context.Trocas
            .Include(t => t.ClienteOfertante)
            .FirstOrDefaultAsync(t => t.Id == trocaId);

        if (troca is null || troca.Status != StatusTroca.Proposta)
            return false;
        if (troca.ClienteOfertante!.ApplicationUserId != userId)   // só o ofertante cancela a própria
            return false;

        troca.Status = StatusTroca.Recusada;
        await _context.SaveChangesAsync();
        return true;
    }

    // Ponte login ↔ loja: acha (ou cria) o Cliente do usuário. (Mesmo padrão de Pedido/Aluguel.)
    private async Task<Cliente> ObterOuCriarClienteAsync(string applicationUserId, string nome)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);
        if (cliente is null)
        {
            cliente = new Cliente { Nome = nome, ApplicationUserId = applicationUserId };
            _context.Clientes.Add(cliente);
        }
        return cliente;
    }
}
