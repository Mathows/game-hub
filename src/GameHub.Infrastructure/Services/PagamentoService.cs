using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Confirma pagamentos (chamado pelo webhook). Scoped, pois usa o GameHubDbContext.
/// </summary>
public class PagamentoService : IPagamentoService
{
    private readonly GameHubDbContext _context;

    public PagamentoService(GameHubDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ConfirmarPagamentoAsync(int pedidoId)
    {
        var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido is null)
            return false;

        // IDEMPOTÊNCIA: um webhook pode ser entregue mais de uma vez. Se já está Pago,
        // não fazemos nada de novo (e não dá erro) — apenas confirmamos que está ok.
        if (pedido.Status == StatusPedido.Pago)
            return true;

        pedido.Status = StatusPedido.Pago;
        await _context.SaveChangesAsync();
        return true;
    }
}
