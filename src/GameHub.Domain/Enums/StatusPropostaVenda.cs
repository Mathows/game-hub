namespace GameHub.Domain.Enums;

/// <summary>
/// Estados da proposta de venda (cliente vende um jogo PARA a loja).
/// É uma MÁQUINA DE ESTADOS: só algumas transições são válidas, e quem valida é o
/// serviço (servidor) — a tela apenas oferece os botões.
///
///   Proposta ──→ EmAvaliacao ──→ Aprovada
///      │              └────────→ Recusada
///      └─→ Cancelada (pelo próprio cliente, antes da avaliação)
/// </summary>
public enum StatusPropostaVenda
{
    Proposta = 1,      // cliente enviou; aguardando a loja
    EmAvaliacao = 2,   // admin pegou pra analisar
    Aprovada = 3,      // loja aceitou: jogo entra no estoque + crédito ao cliente
    Recusada = 4,      // loja recusou (com motivo)
    Cancelada = 5      // o próprio cliente desistiu antes da avaliação
}
