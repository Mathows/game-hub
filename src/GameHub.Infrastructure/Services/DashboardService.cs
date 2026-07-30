using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Consultas AGREGADAS do dashboard. O ponto didático: o GroupBy/Sum/Count roda NO SQL
/// (o LINQ vira SELECT SUM(...)/GROUP BY) — o banco faz a matemática e devolve só o
/// resultado. Trazer mil linhas pra somar em C# seria desperdício de rede e memória.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly GameHubDbContext _context;

    public DashboardService(GameHubDbContext context) => _context = context;

    public async Task<DashboardResumo> ObterResumoAsync()
    {
        // "Dinheiro de verdade" = pedidos PAGOS (pendente ainda não é faturamento).
        var pagos = _context.Pedidos.Where(p => p.Status == StatusPedido.Pago);

        var faturamento = await pagos.SumAsync(p => (decimal?)p.ValorTotal) ?? 0m;
        var qtdPagos = await pagos.CountAsync();
        var ticketMedio = qtdPagos == 0 ? 0m : Math.Round(faturamento / qtdPagos, 2);
        var descontos = await pagos.SumAsync(p => (decimal?)p.Desconto) ?? 0m;

        var pendentes = await _context.Pedidos.CountAsync(p => p.Status == StatusPedido.Pendente);
        var alugueisAtivos = await _context.Alugueis.CountAsync(a => a.Status == StatusAluguel.Ativo);
        var trocasAbertas = await _context.Trocas.CountAsync(t => t.Status == StatusTroca.Proposta);
        var propostasPendentes = await _context.PropostasVenda.CountAsync(p =>
            p.Status == StatusPropostaVenda.Proposta || p.Status == StatusPropostaVenda.EmAvaliacao);

        // TOP JOGOS: GroupBy no servidor — o SQL agrupa por título e soma unidades/receita.
        // PEGADINHA de EF aprendida aqui: agregação de grupo + CONSTRUTOR parametrizado
        // (new TopJogo(...)) o EF NÃO traduz pra SQL ("The LINQ expression could not be
        // translated"). Correção: projetar em TIPO ANÔNIMO (traduzível), materializar
        // (ToListAsync) e SÓ ENTÃO montar o record em memória.
        var topJogosDb = await _context.ItensPedido
            .Where(i => i.Pedido!.Status == StatusPedido.Pago)
            .GroupBy(i => i.Jogo!.Titulo)
            .Select(g => new
            {
                Titulo = g.Key,
                Unidades = g.Sum(i => i.Quantidade),
                Receita = g.Sum(i => i.PrecoUnitario * i.Quantidade)
            })
            .OrderByDescending(t => t.Unidades)
            .Take(5)
            .ToListAsync();
        var topJogos = topJogosDb.Select(t => new TopJogo(t.Titulo, t.Unidades, t.Receita)).ToList();

        // Estoque baixo: candidatos a reposição (≤ 3 unidades). Mesmo padrão anônimo→record.
        var estoqueBaixoDb = await _context.Jogos
            .Where(j => j.QuantidadeEstoque <= 3)
            .OrderBy(j => j.QuantidadeEstoque)
            .Select(j => new { j.Titulo, j.QuantidadeEstoque })
            .Take(5)
            .ToListAsync();
        var estoqueBaixo = estoqueBaixoDb.Select(e => new EstoqueBaixo(e.Titulo, e.QuantidadeEstoque)).ToList();

        return new DashboardResumo(
            faturamento, ticketMedio, qtdPagos, pendentes, descontos,
            alugueisAtivos, trocasAbertas, propostasPendentes, topJogos, estoqueBaixo);
    }
}
