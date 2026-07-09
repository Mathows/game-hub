namespace GameHub.Domain.Services;

/// <summary>
/// Calcula valores e datas de um aluguel. É um serviço SEM ESTADO — só faz conta,
/// não guarda nada e não toca no banco. Por isso é registrado como <b>Transient</b>:
/// pode nascer e "morrer" a cada uso, sem problema (não há o que reaproveitar).
/// </summary>
public class CalculadoraAluguel
{
    /// <summary>Valor do aluguel = preço por dia × número de dias.</summary>
    public decimal CalcularValor(decimal precoPorDia, int dias)
        => precoPorDia * Normalizar(dias);

    /// <summary>Data prevista de devolução = início + dias.</summary>
    public DateTime CalcularDevolucao(DateTime inicio, int dias)
        => inicio.AddDays(Normalizar(dias));

    // Garante pelo menos 1 dia (evita aluguel de 0 dias).
    private static int Normalizar(int dias) => dias < 1 ? 1 : dias;
}
