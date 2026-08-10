namespace GameHub.Domain.Enums;

/// <summary>Onde a encomenda está — derivado do TEMPO (não é um campo gravado no banco).</summary>
public enum StatusRastreio
{
    AguardandoPagamento = 0,   // pedido pendente: nem foi postado
    Postado = 1,
    EmTransito = 2,
    SaiuParaEntrega = 3,
    Entregue = 4
}
