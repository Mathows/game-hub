namespace GameHub.Domain.Interfaces;

/// <summary>Resultado de validar um cupom: se vale, a mensagem pro cliente e o desconto calculado.</summary>
public record ResultadoCupom(bool Valido, string Mensagem, decimal Desconto);

/// <summary>
/// Valida um cupom e calcula o desconto para um subtotal — usado pela PRÉVIA do carrinho.
/// Atenção: a prévia é cortesia de UX; a validação que VALE é a do PedidoService,
/// refeita dentro da transação do pedido (o cupom pode expirar/esgotar entre a prévia e o fechar).
/// </summary>
public interface ICupomService
{
    Task<ResultadoCupom> ValidarAsync(string codigo, decimal subtotal);
}
