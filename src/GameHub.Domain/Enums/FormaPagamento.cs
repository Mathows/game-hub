namespace GameHub.Domain.Enums;

/// <summary>
/// Como o cliente escolheu pagar (a pergunta que todo checkout de verdade faz).
/// Cada forma tem seu fluxo: PIX gera o copia-e-cola, boleto gera a linha digitável,
/// cartão vai direto pro gateway. É ENUM (fluxo fixo do sistema, não lista editável).
/// </summary>
public enum FormaPagamento
{
    Pix = 1,
    Boleto = 2,
    Cartao = 3
}
