using GameHub.Domain.Enums;

namespace GameHub.Domain.Entities;

/// <summary>Uma COMPRA feita por um cliente. Pode conter vários jogos (itens).</summary>
public class Pedido
{
    public int Id { get; set; }

    // A qual cliente pertence este pedido
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime DataPedido { get; set; } = DateTime.Now;
    public StatusPedido Status { get; set; } = StatusPedido.Pendente;
    public decimal ValorTotal { get; set; }

    // Os itens (jogos) deste pedido — um pedido tem vários itens.
    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
}
