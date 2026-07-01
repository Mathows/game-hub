namespace ProGames.Domain.Entities;

/// <summary>
/// Cada jogo DENTRO de um pedido, com quantidade e preço.
/// É a tabela do "meio" que liga Pedido e Jogo (um pedido tem vários itens,
/// e cada item aponta para um jogo).
/// </summary>
public class ItemPedido
{
    public int Id { get; set; }

    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    public int Quantidade { get; set; } = 1;
    public decimal PrecoUnitario { get; set; }  // preço "congelado" no momento da compra
}
