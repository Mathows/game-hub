namespace GameHub.Domain.Entities;

/// <summary>
/// A PROVA de que uma pessoa aceitou uma versão específica do termo.
///
/// Um checkbox marcado no formulário não prova nada — ele desaparece assim que a requisição
/// termina. O que prova é esta linha: QUEM (o usuário), O QUÊ (a versão exata do termo),
/// QUANDO (data e hora) e DE ONDE (IP e navegador).
///
/// Sobre guardar IP e User-Agent: são dados pessoais, sim. Mas são coletados exatamente
/// PARA cumprir uma obrigação — demonstrar o consentimento (LGPD art. 8º, §1º: cabe ao
/// controlador o ônus da prova). Aqui a finalidade justifica a coleta, e é o oposto de
/// coletar "porque pode ser útil um dia".
/// </summary>
public class AceiteTermo
{
    public int Id { get; set; }

    /// <summary>Id do usuário em AspNetUsers. Guardado como texto solto (sem FK) de propósito:
    /// o aceite deve sobreviver ao encerramento da conta — senão a prova morre junto com
    /// o titular e a loja fica sem como demonstrar que houve consentimento.</summary>
    public string ApplicationUserId { get; set; } = string.Empty;

    public int TermoDeUsoId { get; set; }
    public TermoDeUso? Termo { get; set; }

    public DateTime AceitoEm { get; set; } = DateTime.Now;

    /// <summary>IP de origem — parte da prova do consentimento.</summary>
    public string? IpOrigem { get; set; }

    /// <summary>Navegador/dispositivo que enviou o aceite.</summary>
    public string? UserAgent { get; set; }
}
