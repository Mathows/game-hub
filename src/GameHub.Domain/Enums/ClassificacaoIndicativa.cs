namespace GameHub.Domain.Enums;

/// <summary>
/// Classificação indicativa brasileira (Ministério da Justiça) — a faixa etária do jogo.
///
/// O VALOR do enum é a própria idade mínima. Não é enfeite: permite comparar direto
/// (idade >= (int)classificacao) sem tabela de conversão nem switch. Quando o valor
/// numérico do enum TEM significado no domínio, use-o — é o mesmo raciocínio do
/// DigitoCep na tabela de frete.
/// </summary>
public enum ClassificacaoIndicativa
{
    Livre = 0,
    Dez = 10,
    Doze = 12,
    Quatorze = 14,
    Dezesseis = 16,
    Dezoito = 18
}
