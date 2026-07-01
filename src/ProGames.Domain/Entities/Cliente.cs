namespace ProGames.Domain.Entities;

/// <summary>
/// Cliente da loja. Fica LIGADO ao usuário de login (ASP.NET Identity)
/// através do Id do usuário — é a "ponte" entre a loja e o sistema de login.
/// </summary>
public class Cliente
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Endereco { get; set; }

    /// <summary>Id do usuário na tabela AspNetUsers (login). Liga o cliente à conta.</summary>
    public string? ApplicationUserId { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // Um cliente tem vários pedidos, aluguéis e trocas (navegações um-para-muitos).
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
    public ICollection<Troca> Trocas { get; set; } = new List<Troca>();
}
