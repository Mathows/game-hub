using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Regras de negócio das TROCAS entre clientes (Fase 5).
/// Na Passo 1 é implementado com EF Core; na Passo 2 refazemos com NHibernate p/ comparar.
/// </summary>
public interface ITrocaService
{
    /// <summary>Cria uma proposta: ofereço um jogo e desejo outro.</summary>
    Task<Troca> PropoAsync(string ofertanteUserId, string nome, int jogoOferecidoId, int jogoDesejadoId);

    /// <summary>Propostas ABERTAS de OUTROS clientes (que eu posso aceitar).</summary>
    Task<List<Troca>> ObterAbertasAsync(string userId);

    /// <summary>Minhas trocas (onde sou ofertante OU receptor).</summary>
    Task<List<Troca>> ObterMinhasAsync(string userId);

    /// <summary>Aceito uma proposta (viro o receptor). Retorna false se não for possível.</summary>
    Task<bool> AceitarAsync(int trocaId, string userId, string nome);

    /// <summary>Cancelo/recuso uma proposta minha (ofertante).</summary>
    Task<bool> RecusarAsync(int trocaId, string userId);
}
