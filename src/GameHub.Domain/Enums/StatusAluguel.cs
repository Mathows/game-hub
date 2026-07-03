namespace GameHub.Domain.Enums;

/// <summary>Situação de um aluguel de jogo.</summary>
public enum StatusAluguel
{
    Ativo = 1,      // com o cliente, dentro do prazo
    Devolvido = 2,  // já devolvido
    Atrasado = 3    // passou da data prevista
}
