using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Domain.Interfaces;

/// <summary>
/// Workflow de venda pra loja. As TRANSIÇÕES DE ESTADO são validadas aqui (servidor):
/// a tela só oferece botões; quem decide se a transição é permitida é o serviço.
/// </summary>
public interface IPropostaVendaService
{
    /// <summary>Cliente propõe vender uma cópia (get-or-create do Cliente, como sempre).</summary>
    Task<PropostaVenda> PropoAsync(string applicationUserId, string nomeCliente,
        int jogoId, CondicaoJogo condicao, decimal valorPedido, string? observacao);

    /// <summary>Propostas do próprio cliente (mais recentes primeiro).</summary>
    Task<List<PropostaVenda>> MinhasAsync(string applicationUserId);

    /// <summary>Cliente cancela a própria proposta — só enquanto ainda está em "Proposta".</summary>
    Task CancelarAsync(int propostaId, string applicationUserId);

    // ---- Lado do ADMIN ----

    /// <summary>Todas as propostas (admin), pendentes primeiro.</summary>
    Task<List<PropostaVenda>> TodasAsync();

    /// <summary>Proposta → EmAvaliacao (admin pegou para analisar).</summary>
    Task IniciarAvaliacaoAsync(int propostaId);

    /// <summary>
    /// Aprova: define o valor pago (pode contrapor o pedido), dá ENTRADA no estoque
    /// via extrato (P3) e registra a resposta — tudo numa transação.
    /// Retorna a proposta com o Cliente carregado (para a tela notificar o dono).
    /// </summary>
    Task<PropostaVenda> AprovarAsync(int propostaId, decimal valorAprovado, string? resposta);

    /// <summary>Recusa, com motivo obrigatório (o cliente merece saber por quê).</summary>
    Task<PropostaVenda> RecusarAsync(int propostaId, string motivo);
}
