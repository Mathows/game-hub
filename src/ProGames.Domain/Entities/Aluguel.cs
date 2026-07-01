using ProGames.Domain.Enums;

namespace ProGames.Domain.Entities;

/// <summary>Aluguel de um jogo por um cliente, com datas e valor.</summary>
public class Aluguel
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    public DateTime DataInicio { get; set; } = DateTime.Now;
    public DateTime DataPrevistaDevolucao { get; set; }
    public DateTime? DataDevolucao { get; set; }   // fica nulo até o cliente devolver

    public decimal ValorTotal { get; set; }
    public StatusAluguel Status { get; set; } = StatusAluguel.Ativo;
}
