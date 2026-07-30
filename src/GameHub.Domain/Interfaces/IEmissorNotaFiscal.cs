using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Orquestra a emissão: cria/atualiza a NotaFiscal, chama o provider e registra o
/// histórico de status — tudo em transação. Quem "fala com o fisco" é o provider;
/// quem cuida do NOSSO banco (número sequencial, status, histórico) é o emissor.
/// </summary>
public interface IEmissorNotaFiscal
{
    /// <summary>
    /// Emite (ou reemite, se Rejeitada) a nota do pedido. Idempotente: pedido já com nota
    /// Autorizada só a devolve. Exige pedido PAGO.
    /// </summary>
    Task<NotaFiscal> EmitirParaPedidoAsync(int pedidoId);

    /// <summary>Reemissão pedida PELO CLIENTE (valida que o pedido é dele antes).</summary>
    Task<NotaFiscal> ReemitirAsync(int pedidoId, string applicationUserId);

    /// <summary>A nota do pedido (com histórico e itens), se o pedido for do usuário.</summary>
    Task<NotaFiscal?> ObterPorPedidoAsync(int pedidoId, string applicationUserId);
}
