using Microsoft.Extensions.Logging;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// "Envia" e-mails apenas REGISTRANDO (log + lista em memória). É registrado como
/// <b>Singleton</b>: é uma caixa de saída ÚNICA do app, sem estado por usuário.
///
/// Como é Singleton (compartilhado por todas as threads/requisições ao mesmo tempo),
/// a lista precisa de trava (lock) — senão duas requisições gravando juntas dariam erro.
/// Isso ilustra: Singleton com estado mutável exige cuidado com concorrência.
/// </summary>
public class EmailSimuladoService : IEmailService
{
    private readonly ILogger<EmailSimuladoService> _log;
    private readonly List<EmailSimulado> _enviados = new();
    private readonly object _trava = new();

    public EmailSimuladoService(ILogger<EmailSimuladoService> log) => _log = log;

    public IReadOnlyList<EmailSimulado> Enviados
    {
        get { lock (_trava) return _enviados.ToList(); }   // cópia protegida pela trava
    }

    public Task EnviarAsync(string destinatario, string assunto, string corpo)
    {
        var email = new EmailSimulado(destinatario, assunto, corpo, DateTime.Now);
        lock (_trava) _enviados.Insert(0, email);          // mais recente primeiro
        _log.LogInformation("📧 E-mail SIMULADO para {Para} — {Assunto}", destinatario, assunto);
        return Task.CompletedTask;
    }
}
