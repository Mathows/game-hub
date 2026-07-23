namespace GameHub.Domain.Enums;

/// <summary>Como o cupom desconta: um percentual do subtotal ou um valor fixo em reais.</summary>
public enum TipoDesconto
{
    Percentual = 1,   // ex.: Valor=10 → 10% de desconto
    ValorFixo = 2     // ex.: Valor=20 → R$ 20,00 de desconto
}
