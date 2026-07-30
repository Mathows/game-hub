using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Uma linha do EXTRATO de estoque — como um extrato bancário: toda mudança no estoque
/// vira um registro com tipo, quantidade (com sinal) e o saldo resultante.
///
/// Lição do legado (Sistema.md §5.2): estoque como "um número que se soma/subtrai" não
/// responde "por que está assim?". Com o extrato, o saldo é EXPLICÁVEL: a soma das
/// movimentações bate com o número — e cada linha diz quem/quando/por quê (IAuditavel).
/// </summary>
public class MovimentacaoEstoque : IAuditavel
{
    public int Id { get; set; }

    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    public TipoMovimentacaoEstoque Tipo { get; set; }

    /// <summary>COM SINAL: positiva entra (+3), negativa sai (−2). Somar o extrato dá o saldo.</summary>
    public int Quantidade { get; set; }

    /// <summary>O estoque logo APÓS este movimento (a coluna "saldo" do extrato bancário).</summary>
    public int EstoqueDepois { get; set; }

    /// <summary>Motivo da movimentação MANUAL (da tabela de lookup). Nulo nos movimentos
    /// automáticos (Venda/Aluguel), onde o próprio Tipo + referência já explicam.</summary>
    public int? MotivoId { get; set; }
    public MotivoMovimentacao? Motivo { get; set; }

    /// <summary>Texto livre complementar (opcional — o motivo estruturado vem da tabela).</summary>
    public string? Observacao { get; set; }

    // Referências: qual pedido/aluguel causou o movimento (nulos em entradas/ajustes manuais).
    public int? PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public int? AluguelId { get; set; }
    public Aluguel? Aluguel { get; set; }

    // --- Auditoria (o "quem" e "quando" de cada linha, preenchido pelo interceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
