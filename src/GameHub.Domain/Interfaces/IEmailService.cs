namespace GameHub.Domain.Interfaces;

/// <summary>Um e-mail "enviado". Na simulação, guardamos numa caixa de saída pra poder ver.</summary>
public record EmailSimulado(string Para, string Assunto, string Corpo, DateTime Quando);

/// <summary>
/// Serviço de envio de e-mail. Por enquanto é SIMULADO (registra numa caixa de saída em
/// memória, sem mandar nada de verdade). Na Fase 6 plugamos o Gmail SMTP real — e a
/// interface NÃO muda, só a implementação. É o valor de depender de uma interface.
/// </summary>
public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string corpo);

    /// <summary>Caixa de saída simulada (para conferirmos o que "foi enviado").</summary>
    IReadOnlyList<EmailSimulado> Enviados { get; }
}
