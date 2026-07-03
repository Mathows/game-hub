namespace GameHub.Domain.Entities;

/// <summary>Gênero do jogo (Ação, RPG, Esporte, Aventura...).</summary>
public class Genero
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    // Um-para-muitos: um gênero tem vários jogos.
    public ICollection<Jogo> Jogos { get; set; } = new List<Jogo>();
}
