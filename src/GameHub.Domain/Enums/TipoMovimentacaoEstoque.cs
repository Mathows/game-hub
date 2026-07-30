namespace GameHub.Domain.Enums;

/// <summary>Por que o estoque mudou. Cada linha do extrato tem um tipo.</summary>
public enum TipoMovimentacaoEstoque
{
    Entrada = 1,           // reposição/compra de mercadoria (quantidade positiva)
    Venda = 2,             // baixa por venda (negativa)
    Aluguel = 3,           // baixa por aluguel — a cópia sai enquanto alugada (negativa)
    DevolucaoAluguel = 4,  // a cópia voltou do aluguel (positiva) — fluxo futuro
    Ajuste = 5             // correção manual do admin (ex.: contagem/perda; pode ser ±)
}
