using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>Um item que o cliente quer ALUGAR: qual jogo e por quantos dias.</summary>
public record ItemAluguel(int JogoId, int Dias);

/// <summary>
/// Contrato do serviço que registra ALUGUÉIS: cria um Aluguel por jogo (com datas e
/// valor calculado) e dá baixa no estoque — tudo dentro de uma transação.
/// </summary>
public interface IAluguelService
{
    Task<List<Aluguel>> FinalizarAluguelAsync(string applicationUserId, string nomeCliente, IReadOnlyList<ItemAluguel> itens);
}
