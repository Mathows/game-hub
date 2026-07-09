using NHibernate;
using NHibernate.Linq;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.NHib;

/// <summary>
/// MESMA interface do TrocaService (EF), mas por baixo usa <b>NHibernate</b>.
/// Compare com o TrocaService do EF: aqui usamos <c>ISession</c> (em vez de DbContext),
/// <c>BeginTransaction/Commit</c>, <c>SaveAsync</c> e consultas LINQ via <c>_session.Query</c>.
/// </summary>
public class TrocaServiceNHibernate : ITrocaService
{
    private readonly ISession _session;

    public TrocaServiceNHibernate(ISession session) => _session = session;

    public async Task<Troca> PropoAsync(string ofertanteUserId, string nome, int jogoOferecidoId, int jogoDesejadoId)
    {
        if (jogoOferecidoId == jogoDesejadoId)
            throw new InvalidOperationException("O jogo oferecido e o desejado precisam ser diferentes.");

        using var tx = _session.BeginTransaction();
        var cliente = await ObterOuCriarClienteAsync(ofertanteUserId, nome);

        var troca = new Troca
        {
            ClienteOfertante = cliente,
            // Load = referência ao jogo pelo id (não precisa ler tudo só para gravar a FK).
            JogoOferecido = await _session.LoadAsync<Jogo>(jogoOferecidoId),
            JogoDesejado = await _session.LoadAsync<Jogo>(jogoDesejadoId),
            Status = StatusTroca.Proposta,
            DataProposta = DateTime.Now
        };

        await _session.SaveAsync(troca);
        await tx.CommitAsync();
        return troca;
    }

    public async Task<List<Troca>> ObterAbertasAsync(string userId)
        => await _session.Query<Troca>()
            .Fetch(t => t.ClienteOfertante)
            .Fetch(t => t.JogoOferecido)
            .Fetch(t => t.JogoDesejado)
            .Where(t => t.Status == StatusTroca.Proposta && t.ClienteOfertante.ApplicationUserId != userId)
            .OrderByDescending(t => t.DataProposta)
            .ToListAsync();

    public async Task<List<Troca>> ObterMinhasAsync(string userId)
        => await _session.Query<Troca>()
            .Fetch(t => t.ClienteOfertante)
            .Fetch(t => t.ClienteReceptor)
            .Fetch(t => t.JogoOferecido)
            .Fetch(t => t.JogoDesejado)
            .Where(t => t.ClienteOfertante.ApplicationUserId == userId
                     || (t.ClienteReceptor != null && t.ClienteReceptor.ApplicationUserId == userId))
            .OrderByDescending(t => t.DataProposta)
            .ToListAsync();

    public async Task<bool> AceitarAsync(int trocaId, string userId, string nome)
    {
        using var tx = _session.BeginTransaction();
        var troca = await _session.GetAsync<Troca>(trocaId);
        if (troca is null || troca.Status != StatusTroca.Proposta) return false;
        if (troca.ClienteOfertante.ApplicationUserId == userId) return false;

        troca.ClienteReceptor = await ObterOuCriarClienteAsync(userId, nome);
        troca.Status = StatusTroca.Aceita;
        await tx.CommitAsync();   // NHibernate grava as alterações no commit (flush automático)
        return true;
    }

    public async Task<bool> RecusarAsync(int trocaId, string userId)
    {
        using var tx = _session.BeginTransaction();
        var troca = await _session.GetAsync<Troca>(trocaId);
        if (troca is null || troca.Status != StatusTroca.Proposta) return false;
        if (troca.ClienteOfertante.ApplicationUserId != userId) return false;

        troca.Status = StatusTroca.Recusada;
        await tx.CommitAsync();
        return true;
    }

    private async Task<Cliente> ObterOuCriarClienteAsync(string applicationUserId, string nome)
    {
        var cliente = await _session.Query<Cliente>()
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);
        if (cliente is null)
        {
            cliente = new Cliente { Nome = nome, ApplicationUserId = applicationUserId, DataCadastro = DateTime.Now };
            await _session.SaveAsync(cliente);
        }
        return cliente;
    }
}
