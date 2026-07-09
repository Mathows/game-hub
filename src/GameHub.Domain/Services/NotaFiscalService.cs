using GameHub.Domain.Entities;

namespace GameHub.Domain.Services;

/// <summary>Uma linha da nota (um jogo comprado).</summary>
public record ItemNota(string Descricao, int Quantidade, decimal ValorUnitario, decimal ValorTotal);

/// <summary>
/// Nota fiscal SIMULADA (não tem validade fiscal — é só o "documento" gerado após o pagamento,
/// para aprender o fluxo). Uma NF-e real exige CNPJ + certificado digital + SEFAZ.
/// </summary>
public record NotaFiscalSimulada(
    string Numero,
    string Serie,
    string ChaveAcesso,
    DateTime DataEmissao,
    string NomeCliente,
    IReadOnlyList<ItemNota> Itens,
    decimal ValorTotal);

/// <summary>
/// Gera a nota fiscal simulada a partir de um pedido. É um serviço SEM ESTADO (só faz conta/
/// formatação), então registramos como <b>Transient</b> — igual à CalculadoraAluguel.
/// </summary>
public class NotaFiscalService
{
    public NotaFiscalSimulada GerarParaPedido(Pedido pedido)
    {
        var itens = pedido.Itens
            .Select(i => new ItemNota(
                Descricao: i.Jogo?.Titulo ?? $"Jogo {i.JogoId}",
                Quantidade: i.Quantidade,
                ValorUnitario: i.PrecoUnitario,
                ValorTotal: i.PrecoUnitario * i.Quantidade))
            .ToList();

        return new NotaFiscalSimulada(
            Numero: pedido.Id.ToString("D9"),          // nº da nota (9 dígitos)
            Serie: "001",
            ChaveAcesso: GerarChaveAcesso(pedido),     // 44 dígitos "no formato" NF-e
            DataEmissao: pedido.DataPedido,
            NomeCliente: pedido.Cliente?.Nome ?? "Consumidor",
            Itens: itens,
            ValorTotal: pedido.ValorTotal);
    }

    // Monta uma "chave de acesso" de 44 dígitos só para PARECER uma NF-e (dados fictícios).
    // Estrutura real: UF(2) AAMM(4) CNPJ(14) modelo(2) série(3) número(9) tpEmis(1) cNF(8) DV(1).
    private static string GerarChaveAcesso(Pedido pedido)
    {
        var aamm = pedido.DataPedido.ToString("yyMM");
        var chave =
            "35" +                       // UF: SP
            aamm +                       // ano/mês
            "00000000000191" +           // CNPJ fictício
            "55" +                       // modelo (55 = NF-e)
            "001" +                      // série
            pedido.Id.ToString("D9") +   // número
            "1" +                        // tipo de emissão
            pedido.Id.ToString("D8") +   // código numérico
            (pedido.Id % 10).ToString(); // dígito verificador (fake)
        return chave;                    // 44 dígitos
    }
}
