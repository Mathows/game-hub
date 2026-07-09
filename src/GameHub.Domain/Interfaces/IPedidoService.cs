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
    Task<Pedido> FinalizarCompraAsync(string applicationUserId, string nomeCliente, IReadOnlyList<ItemCompra> itens);

    /// <summary>Lista os pedidos de um usuário (mais recentes primeiro), com itens e jogos.</summary>
    Task<List<Pedido>> ObterPorUsuarioAsync(string applicationUserId);

    /// <summary>Um pedido específico do usuário (ou null se não existir/não for dele), com itens e jogos.</summary>
    Task<Pedido?> ObterPorIdAsync(int pedidoId, string applicationUserId);
}
