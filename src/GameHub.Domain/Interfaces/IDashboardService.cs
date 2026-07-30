namespace GameHub.Domain.Interfaces;

/// <summary>
/// READ-MODEL do dashboard (Sistema.md §5.2): relatório tem formato PRÓPRIO, separado
/// das entidades de escrita. São só números/projeções — ninguém edita um dashboard.
/// </summary>
public record DashboardResumo(
    decimal FaturamentoTotal,        // soma dos pedidos PAGOS (dinheiro de verdade)
    decimal TicketMedio,             // média por pedido pago
    int PedidosPagos,
    int PedidosPendentes,
    decimal DescontosConcedidos,     // total de cupons aplicados (pedidos pagos)
    int AlugueisAtivos,
    int TrocasAbertas,
    int PropostasPendentes,          // fila do "vender pra loja"
    List<TopJogo> TopJogos,          // mais vendidos (pagos)
    List<EstoqueBaixo> EstoqueBaixo);

public record TopJogo(string Titulo, int Unidades, decimal Receita);
public record EstoqueBaixo(string Titulo, int Quantidade);

/// <summary>Números agregados da loja para o painel do admin.</summary>
public interface IDashboardService
{
    Task<DashboardResumo> ObterResumoAsync();
}
