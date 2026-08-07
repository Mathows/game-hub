using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Domain.Services;

/// <summary>Um evento da linha do tempo do rastreio.</summary>
public record EventoRastreio(DateTime Quando, string Titulo, string Local, bool Concluido);

/// <summary>O rastreio completo: código, status atual, previsão e a linha do tempo.</summary>
public record Rastreio(
    string Codigo,
    StatusRastreio Status,
    DateTime? PrevisaoEntrega,
    IReadOnlyList<EventoRastreio> Eventos);

/// <summary>
/// Rastreio SIMULADO — e o desenho é o ponto interessante: os eventos **não são gravados**
/// no banco; eles são uma FUNÇÃO DO TEMPO, calculada a partir de dois dados que o pedido
/// já tem: a <c>DataPagamento</c> e o <c>PrazoEntregaDias</c> (o prazo prometido).
///
/// Consequência: o rastreio "anda sozinho" conforme os dias passam, sem nenhum job em
/// background — e o histórico é sempre coerente (nada de evento faltando ou duplicado).
///
/// É um serviço SEM ESTADO e SEM I/O (não toca no banco) → registrado como <b>Transient</b>,
/// igual à CalculadoraAluguel. Por ser função pura, é o candidato perfeito para os
/// testes de unidade da Fase 12: entra (data, prazo), sai (eventos).
///
/// Num cenário real, os eventos viriam da transportadora por API/webhook e ficariam
/// persistidos (cada evento com a data real que aconteceu) — o `IFreteService` plugável
/// já deixa esse caminho aberto.
/// </summary>
public class CalculadoraRastreio
{
    /// <summary>Monta o rastreio do pedido no momento <paramref name="agora"/>.</summary>
    public Rastreio Calcular(Pedido pedido, DateTime agora)
    {
        var codigo = GerarCodigo(pedido);

        // Sem pagamento confirmado, não há postagem (é o "aguardando pagamento" das lojas).
        if (pedido.Status != StatusPedido.Pago || pedido.DataPagamento is null)
            return new Rastreio(codigo, StatusRastreio.AguardandoPagamento, null, Array.Empty<EventoRastreio>());

        var pagamento = pedido.DataPagamento.Value;
        var prazo = Math.Max(pedido.PrazoEntregaDias, 1);
        var cidade = pedido.EnderecoEntrega is null
            ? "destino"
            : $"{pedido.EnderecoEntrega.Cidade}/{pedido.EnderecoEntrega.Uf}";

        // Marcos da viagem, distribuídos dentro do prazo prometido.
        var eventos = new List<EventoRastreio>
        {
            Evento(pagamento, "Pagamento aprovado — pedido em separação", "GameHub · São José dos Campos/SP", agora),
            Evento(pagamento.AddDays(1), "Objeto postado", "Agência de origem · São José dos Campos/SP", agora)
        };

        // Só faz sentido mostrar "em trânsito" quando a viagem tem mais de 2 dias.
        if (prazo >= 3)
            eventos.Add(Evento(pagamento.AddDays(prazo - 1), "Objeto em trânsito — chegou à unidade de destino",
                $"Centro de distribuição · {cidade}", agora));

        eventos.Add(Evento(pagamento.AddDays(prazo).Date.AddHours(9), "Objeto saiu para entrega ao destinatário",
            $"Unidade de entrega · {cidade}", agora));
        eventos.Add(Evento(pagamento.AddDays(prazo).Date.AddHours(15), "Objeto entregue ao destinatário",
            cidade, agora));

        // O STATUS é o último evento já concluído — nada de campo gravado que possa "desatualizar".
        var concluidos = eventos.Count(e => e.Concluido);
        var status = concluidos switch
        {
            0 or 1 => StatusRastreio.Postado,          // pago (e/ou postado)
            _ when concluidos == eventos.Count => StatusRastreio.Entregue,
            _ when concluidos == eventos.Count - 1 => StatusRastreio.SaiuParaEntrega,
            _ => StatusRastreio.EmTransito
        };

        var previsao = pagamento.AddDays(prazo).Date.AddHours(15);
        return new Rastreio(codigo, status, previsao, eventos);
    }

    private static EventoRastreio Evento(DateTime quando, string titulo, string local, DateTime agora)
        => new(quando, titulo, local, agora >= quando);

    /// <summary>
    /// Código no formato dos Correios (2 letras + 9 dígitos + BR) — determinístico:
    /// o mesmo pedido sempre gera o mesmo código.
    /// </summary>
    private static string GerarCodigo(Pedido pedido) => $"GH{pedido.Id:D9}BR";
}
