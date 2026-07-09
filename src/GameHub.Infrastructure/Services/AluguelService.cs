using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Domain.Services;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Registra aluguéis usando EF Core. É <b>Scoped</b> (usa o GameHubDbContext) e recebe
/// por Injeção de Dependência a <see cref="CalculadoraAluguel"/> (que é Transient).
/// Repare: um serviço Scoped pode depender de um Transient sem problema.
/// </summary>
public class AluguelService : IAluguelService
{
    private readonly GameHubDbContext _context;
    private readonly CalculadoraAluguel _calculadora;

    public AluguelService(GameHubDbContext context, CalculadoraAluguel calculadora)
    {
        _context = context;
        _calculadora = calculadora;
    }

    public async Task<List<Aluguel>> FinalizarAluguelAsync(string applicationUserId, string nomeCliente, IReadOnlyList<ItemAluguel> itens)
    {
        if (itens is null || itens.Count == 0)
            throw new InvalidOperationException("Não há itens de aluguel no carrinho.");

        await using var transacao = await _context.Database.BeginTransactionAsync();
        try
        {
            // Ponte login ↔ loja: acha (ou cria) o Cliente do usuário logado.
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);
            if (cliente is null)
            {
                cliente = new Cliente { Nome = nomeCliente, ApplicationUserId = applicationUserId };
                _context.Clientes.Add(cliente);
            }

            var alugueis = new List<Aluguel>();
            var agora = DateTime.Now;

            foreach (var item in itens)
            {
                var jogo = await _context.Jogos.FirstOrDefaultAsync(j => j.Id == item.JogoId)
                    ?? throw new InvalidOperationException($"Jogo {item.JogoId} não encontrado.");

                // A cópia física sai do estoque enquanto está alugada.
                if (jogo.QuantidadeEstoque < 1)
                    throw new InvalidOperationException($"\"{jogo.Titulo}\" está sem estoque para aluguel.");
                jogo.QuantidadeEstoque -= 1;

                var aluguel = new Aluguel
                {
                    Cliente = cliente,
                    Jogo = jogo,
                    DataInicio = agora,
                    // Datas e valor vêm da CalculadoraAluguel (Transient).
                    DataPrevistaDevolucao = _calculadora.CalcularDevolucao(agora, item.Dias),
                    ValorTotal = _calculadora.CalcularValor(jogo.PrecoAluguelDia, item.Dias),
                    Status = StatusAluguel.Ativo
                };
                _context.Alugueis.Add(aluguel);
                alugueis.Add(aluguel);
            }

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
            return alugueis;
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }
}
