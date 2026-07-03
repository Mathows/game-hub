namespace GameHub.Domain.Enums;

/// <summary>Situação de uma proposta de troca entre clientes.</summary>
public enum StatusTroca
{
    Proposta = 1,   // troca proposta, aguardando resposta
    Aceita = 2,
    Recusada = 3,
    Concluida = 4
}
