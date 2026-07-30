using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Orquestra a emissão da nota (EF Core, Scoped): número sequencial, chamada ao provider,
/// status + HISTÓRICO de transição — tudo numa transação. O provider é injetado pela
/// interface: simulado hoje, sandbox/real amanhã, e esta classe NÃO muda.
/// </summary>
public class EmissorNotaFiscal : IEmissorNotaFiscal
{
    private readonly GameHubDbContext _context;
    private readonly INotaFiscalProvider _provider;

    public EmissorNotaFiscal(GameHubDbContext context, INotaFiscalProvider provider)
    {
        _context = context;
        _provider = provider;
    }

    public async Task<NotaFiscal> EmitirParaPedidoAsync(int pedidoId)
    {
        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Itens).ThenInclude(i => i.Jogo)
                .FirstOrDefaultAsync(p => p.Id == pedidoId)
                ?? throw new InvalidOperationException($"Pedido {pedidoId} não encontrado.");

            // Regra de negócio: nota só existe para pedido PAGO.
            if (pedido.Status != StatusPedido.Pago)
                throw new InvalidOperationException("A nota fiscal só é emitida após o pagamento.");

            var nota = await _context.NotasFiscais
                .Include(n => n.Historico)
                .FirstOrDefaultAsync(n => n.PedidoId == pedidoId);

            // Idempotência: já autorizada (ou cancelada) → devolve como está, sem reemitir.
            if (nota is not null && nota.Status is StatusNotaFiscal.Autorizada or StatusNotaFiscal.Cancelada)
            {
                await transacao.RollbackAsync();
                return nota;
            }

            if (nota is null)
            {
                // Número sequencial por série — o "NossoNumero" fiscal (dentro da transação!).
                var proximoNumero = (await _context.NotasFiscais
                    .Where(n => n.Serie == 1)
                    .MaxAsync(n => (int?)n.Numero) ?? 0) + 1;

                nota = new NotaFiscal
                {
                    Pedido = pedido,
                    Numero = proximoNumero,
                    Serie = 1,
                    ValorTotal = pedido.ValorTotal,
                    DataEmissao = DateTime.Now,
                    Status = StatusNotaFiscal.Pendente
                };
                nota.Historico.Add(new HistoricoStatusNota
                {
                    StatusAnterior = null,
                    StatusNovo = StatusNotaFiscal.Pendente,
                    Observacao = $"Nota criada para o pedido #{pedido.Id}."
                });
                _context.NotasFiscais.Add(nota);
            }

            // Chama o "fisco" (provider plugável) e registra a TRANSIÇÃO de status.
            var statusAnterior = nota.Status;
            var resultado = await _provider.EmitirAsync(nota, pedido);

            if (resultado.Autorizada)
            {
                nota.Status = StatusNotaFiscal.Autorizada;
                nota.ChaveAcesso = resultado.ChaveAcesso;
                nota.Protocolo = resultado.Protocolo;
                nota.DataAutorizacao = DateTime.Now;
                nota.MotivoRejeicao = null;
                nota.Historico.Add(new HistoricoStatusNota
                {
                    StatusAnterior = statusAnterior,
                    StatusNovo = StatusNotaFiscal.Autorizada,
                    Observacao = $"Autorizada — protocolo {resultado.Protocolo}."
                });
            }
            else
            {
                nota.Status = StatusNotaFiscal.Rejeitada;
                nota.MotivoRejeicao = resultado.MotivoRejeicao;
                nota.Historico.Add(new HistoricoStatusNota
                {
                    StatusAnterior = statusAnterior,
                    StatusNovo = StatusNotaFiscal.Rejeitada,
                    Observacao = resultado.MotivoRejeicao
                });
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return nota;
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }

    public async Task<NotaFiscal> ReemitirAsync(int pedidoId, string applicationUserId)
    {
        // Só o DONO do pedido reemite (validação no servidor, como sempre).
        var ehDono = await _context.Pedidos.AnyAsync(p =>
            p.Id == pedidoId && p.Cliente!.ApplicationUserId == applicationUserId);
        if (!ehDono)
            throw new InvalidOperationException("Pedido não encontrado.");

        return await EmitirParaPedidoAsync(pedidoId);
    }

    public async Task<NotaFiscal?> ObterPorPedidoAsync(int pedidoId, string applicationUserId)
        => await _context.NotasFiscais.AsNoTracking()
            .Include(n => n.Historico)
            .Include(n => n.Pedido!).ThenInclude(p => p.Cliente)
            .Include(n => n.Pedido!).ThenInclude(p => p.Itens).ThenInclude(i => i.Jogo)
            .FirstOrDefaultAsync(n => n.PedidoId == pedidoId
                && n.Pedido!.Cliente!.ApplicationUserId == applicationUserId);
}
