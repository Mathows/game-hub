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

    /// <summary>Frete calculado NO SERVIDOR a partir do endereço de entrega (já somado no total).</summary>
    public decimal ValorFrete { get; set; }

    /// <summary>Quando o pagamento foi confirmado (base do rastreio simulado da Fase 10).</summary>
    public DateTime? DataPagamento { get; set; }

    // Endereço de ENTREGA: um snapshot (owned type) copiado no checkout.
    // 1 pedido = 1 endereço, garantido pelo schema (ver Sistema.md §5.1).
    // Nulo enquanto o pedido não tem entrega definida (ex.: pedidos antigos).
    public EnderecoEntrega? EnderecoEntrega { get; set; }

    // Como o cliente escolheu pagar (Fase 9). Nulo nos pedidos de antes da feature.
    public FormaPagamento? FormaPagamento { get; set; }

    // A cobrança gerada no checkout (1:1 — navegação para a tela mostrar o "como pagar").
    public Cobranca? Cobranca { get; set; }

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
