using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>Uma COMPRA feita por um cliente. Pode conter vários jogos (itens).</summary>
public class Pedido : IAuditavel
{
    public int Id { get; set; }

    // A qual cliente pertence este pedido
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime DataPedido { get; set; } = DateTime.Now;
    public StatusPedido Status { get; set; } = StatusPedido.Pendente;
    public decimal ValorTotal { get; set; }

    // Endereço de ENTREGA: um snapshot (owned type) copiado no checkout.
    // 1 pedido = 1 endereço, garantido pelo schema (ver Sistema.md §5.1).
    // Nulo enquanto o pedido não tem entrega definida (ex.: pedidos antigos).
    public EnderecoEntrega? EnderecoEntrega { get; set; }

    // Cupom aplicado (se houver) + o desconto CONGELADO em R$ no momento da compra.
    // Guardamos o valor calculado (snapshot) — se o cupom mudar depois, o pedido não muda.
    public int? CupomId { get; set; }
    public Cupom? Cupom { get; set; }
    public decimal Desconto { get; set; }

    // Os itens (jogos) deste pedido — um pedido tem vários itens.
    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
