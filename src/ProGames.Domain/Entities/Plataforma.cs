namespace ProGames.Domain.Entities;

/// <summary>Plataforma do jogo (PC, PlayStation, Xbox, Nintendo Switch...).</summary>
public class Plataforma
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // Um-para-muitos: uma plataforma tem VÁRIOS jogos.
    // Esta lista é a "navegação" — não vira coluna, o EF a preenche pelos relacionamentos.
    public ICollection<Jogo> Jogos { get; set; } = new List<Jogo>();
}
