using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Envio de e-mail REAL via Gmail SMTP (usa MailKit — a lib recomendada pela Microsoft;
/// o velho System.Net.Mail.SmtpClient é considerado obsoleto).
///
/// Mesma interface do simulado (<see cref="IEmailService"/>): as telas e o PagamentoService
/// NÃO mudam — só a implementação registrada na DI. É o "programar contra interface" valendo.
///
/// Autenticação: e-mail + SENHA DE APP do Google (não a senha normal da conta; exige
/// verificação em 2 etapas). Credenciais vêm de user-secrets — NUNCA do Git.
///
/// Também é Singleton e mantém a caixa de saída em memória (com lock, mesma lição de
/// concorrência do simulado) — assim a página /emails vira o histórico do que FOI enviado.
/// </summary>
public class GmailSmtpService : IEmailService
{
    private const string HostSmtp = "smtp.gmail.com";
    private const int PortaSmtp = 587;                    // STARTTLS

    private readonly string _usuario;                     // o Gmail remetente
    private readonly string _senhaApp;                    // senha de app (16 letras)
    private readonly ILogger<GmailSmtpService> _log;

    private readonly List<EmailSimulado> _enviados = new();
    private readonly object _trava = new();

    public GmailSmtpService(string usuario, string senhaApp, ILogger<GmailSmtpService> log)
    {
        _usuario = usuario;
        _senhaApp = senhaApp;
        _log = log;
    }

    public IReadOnlyList<EmailSimulado> Enviados
    {
        get { lock (_trava) return _enviados.ToList(); }
    }

    public async Task EnviarAsync(string destinatario, string assunto, string corpo)
    {
        // Monta a mensagem (MimeKit é o formato de e-mail do MailKit).
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress("GameHub", _usuario));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;
        mensagem.Body = new TextPart("plain") { Text = corpo };

        // Conecta, autentica, envia, desconecta — o ciclo SMTP completo.
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(HostSmtp, PortaSmtp, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_usuario, _senhaApp);
        await smtp.SendAsync(mensagem);
        await smtp.DisconnectAsync(quit: true);

        lock (_trava) _enviados.Insert(0, new EmailSimulado(destinatario, assunto, corpo, DateTime.Now));
        _log.LogInformation("📧 E-mail REAL enviado via Gmail para {Para} — {Assunto}", destinatario, assunto);
    }
}
