using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Confirma pagamentos (chamado pelo webhook). Scoped, pois usa o GameHubDbContext.
/// Ao confirmar: dispara o e-mail de confirmação E emite a nota fiscal (Fase 8).
/// </summary>
public class PagamentoService : IPagamentoService
{
    private readonly GameHubDbContext _context;
    private readonly IEmailService _email;
    private readonly IEmissorNotaFiscal _emissorNota;
    private readonly ILogger<PagamentoService> _log;

    public PagamentoService(GameHubDbContext context, IEmailService email,
        IEmissorNotaFiscal emissorNota, ILogger<PagamentoService> log)
    {
        _context = context;
        _email = email;
        _emissorNota = emissorNota;
        _log = log;
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

        // "Pagou → emite a nota" (evento de negócio, Fase 8). Falha fiscal NÃO desfaz o
        // pagamento (o dinheiro entrou!): se der erro, loga e a nota pode ser reemitida.
        try
        {
            var nota = await _emissorNota.EmitirParaPedidoAsync(pedido.Id);
            _log.LogInformation("Nota fiscal do pedido #{Pedido}: {Status} (nº {Numero}).",
                pedido.Id, nota.Status, nota.Numero);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Falha ao emitir a nota do pedido #{Pedido} — reemitir depois.", pedido.Id);
        }

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
