using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>Um item que o cliente quer COMPRAR: qual jogo e quantas unidades.</summary>
public record ItemCompra(int JogoId, int Quantidade);

/// <summary>
/// Contrato do serviço que FECHA uma compra: cria o Pedido, seus itens e dá baixa
/// no estoque — tudo dentro de uma TRANSAÇÃO (ou tudo é gravado, ou nada é).
/// Fica em Domain (o "o quê"); a implementação com EF Core fica em Infrastructure.
/// </summary>
public interface IPedidoService
{
    /// <param name="enderecoEntrega">
    /// Snapshot do endereço de entrega escolhido no checkout (cópia — ver Sistema.md §5.1).
    /// Pode ser null (ex.: carrinho sem endereço definido).
    /// </param>
    /// <param name="cupomCodigo">
    /// CÓDIGO do cupom (ou null). Só o código: quem valida e calcula o desconto é o servidor.
    /// </param>
    Task<Pedido> FinalizarCompraAsync(string applicationUserId, string nomeCliente, IReadOnlyList<ItemCompra> itens, EnderecoEntrega? enderecoEntrega, string? cupomCodigo = null);

    /// <summary>Lista os pedidos de um usuário (mais recentes primeiro), com itens e jogos.</summary>
    Task<List<Pedido>> ObterPorUsuarioAsync(string applicationUserId);

    /// <summary>Um pedido específico do usuário (ou null se não existir/não for dele), com itens e jogos.</summary>
    Task<Pedido?> ObterPorIdAsync(int pedidoId, string applicationUserId);
}
