namespace GameHub.Domain.Interfaces;

/// <summary>
/// Serviço acionado pelo webhook de pagamento. Confirma o pagamento de um pedido
/// (marca como Pago). É o que dá sentido ao Status inicial "Pendente".
/// </summary>
public interface IPagamentoService
{
    /// <summary>Marca o pedido como Pago. Retorna false se o pedido não existir.</summary>
    Task<bool> ConfirmarPagamentoAsync(int pedidoId);
}
