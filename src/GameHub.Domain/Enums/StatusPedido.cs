namespace GameHub.Domain.Enums;

/// <summary>Situação de uma compra (Pedido) ao longo do tempo.</summary>
public enum StatusPedido
{
    Pendente = 1,   // aguardando pagamento
    Pago = 2,       // pagamento confirmado (será feito via webhook, na Fase 4)
    Enviado = 3,    // pedido despachado
    Cancelado = 4
}
