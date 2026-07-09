using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Confirma pagamentos (chamado pelo webhook). Scoped, pois usa o GameHubDbContext.
/// Ao confirmar, dispara o e-mail de confirmação (IEmailService).
/// </summary>
public class PagamentoService : IPagamentoService
{
    private readonly GameHubDbContext _context;
    private readonly IEmailService _email;

    public PagamentoService(GameHubDbContext context, IEmailService email)
    {
        _context = context;
        _email = email;
    }

    public async Task<bool> ConfirmarPagamentoAsync(int pedidoId)
    {
        // Traz o pedido com cliente e itens (precisamos deles para montar o e-mail).
        var pedido = await _context.Pedidos
            .Include(p => p.Cliente)
            .Include(p => p.Itens).ThenInclude(i => i.Jogo)
            .FirstOrDefaultAsync(p => p.Id == pedidoId);

        if (pedido is null)
            return false;

        // IDEMPOTÊNCIA: um webhook pode chegar mais de uma vez. Se já está Pago,
        // saímos aqui — e, de quebra, o e-mail NÃO é reenviado.
        if (pedido.Status == StatusPedido.Pago)
            return true;

        pedido.Status = StatusPedido.Pago;
        await _context.SaveChangesAsync();

        // Pagamento confirmado → envia o e-mail de confirmação (só na 1ª vez).
        await EnviarConfirmacaoAsync(pedido);
        return true;
    }

    private async Task EnviarConfirmacaoAsync(Domain.Entities.Pedido pedido)
    {
        // O nome do cliente hoje guarda o e-mail do login (definido ao criar o Cliente).
        var destino = pedido.Cliente?.Nome ?? "cliente";
        var assunto = $"GameHub — Pedido #{pedido.Id} confirmado!";

        var linhas = pedido.Itens
            .Select(i => $"- {i.Jogo?.Titulo} x{i.Quantidade} = R$ {(i.PrecoUnitario * i.Quantidade):N2}");

        var corpo =
            $"Olá!\n\n" +
            $"Seu pagamento foi aprovado. Confira o pedido #{pedido.Id}:\n" +
            string.Join("\n", linhas) + "\n\n" +
            $"Total: R$ {pedido.ValorTotal:N2}\n\n" +
            $"Obrigado por comprar na GameHub! 🎮";

        await _email.EnviarAsync(destino, assunto, corpo);
    }
}
