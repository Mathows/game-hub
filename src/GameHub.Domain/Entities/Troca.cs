using GameHub.Domain.Enums;

namespace GameHub.Domain.Entities;

/// <summary>
/// Proposta de TROCA: um cliente oferece um jogo e deseja outro em troca.
/// Repare que esta entidade aponta para DOIS jogos diferentes.
/// </summary>
public class Troca
{
    public int Id { get; set; }

    // Cliente que propôs a troca
    public int ClienteOfertanteId { get; set; }
    public Cliente? ClienteOfertante { get; set; }

    // Cliente que ACEITOU a troca (fica nulo até alguém aceitar) — é a "outra ponta".
    public int? ClienteReceptorId { get; set; }
    public Cliente? ClienteReceptor { get; set; }

    // Jogo que ele OFERECE
    public int JogoOferecidoId { get; set; }
    public Jogo? JogoOferecido { get; set; }

    // Jogo que ele QUER receber
    public int JogoDesejadoId { get; set; }
    public Jogo? JogoDesejado { get; set; }

    public StatusTroca Status { get; set; } = StatusTroca.Proposta;
    public DateTime DataProposta { get; set; } = DateTime.Now;
}
