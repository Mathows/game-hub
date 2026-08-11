using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Consentimento versionado: qual termo está em vigor, quem já aceitou e quem precisa
/// aceitar de novo porque o texto mudou.
/// </summary>
public interface IConsentimentoService
{
    /// <summary>A versão em vigor (a que novos usuários precisam aceitar).</summary>
    Task<TermoDeUso?> ObterVigenteAsync();

    /// <summary>Todas as versões já publicadas, da mais nova para a mais antiga.</summary>
    Task<List<TermoDeUso>> ObterHistoricoAsync();

    /// <summary>Grava o aceite com data, IP e navegador (a prova).</summary>
    Task RegistrarAceiteAsync(string applicationUserId, int termoId, string? ip, string? userAgent);

    /// <summary>
    /// true quando o usuário NÃO aceitou a versão vigente — porque nunca aceitou nada
    /// ou porque só aceitou uma versão anterior. É o gatilho do pedido de novo aceite.
    /// </summary>
    Task<bool> PrecisaAceitarAsync(string applicationUserId);

    /// <summary>Histórico de aceites de um usuário (entra na exportação de dados do P1).</summary>
    Task<List<AceiteTermo>> AceitesDoUsuarioAsync(string applicationUserId);
}
