namespace GameHub.Domain.Interfaces;

/// <summary>
/// Serviços de LGPD sobre os dados do titular que vivem na LOJA (fora do Identity).
///
/// POR QUE ISSO EXISTE: o ASP.NET Identity já tem telas de "baixar/excluir meus dados",
/// mas ele só conhece o mundo dele (AspNetUsers). Ele NÃO sabe que existe um Cliente com
/// CPF, uma agenda de endereços, pedidos e notas fiscais. Sem este serviço, o "baixar"
/// entrega dados pela metade e o "excluir" deixa o dado pessoal todo no banco.
/// </summary>
public interface IDadosPessoaisService
{
    /// <summary>
    /// Monta o pacote com TUDO o que a loja guarda sobre o titular
    /// (direito de acesso — LGPD art. 18, II).
    /// </summary>
    Task<DadosPessoaisExportados> ExportarAsync(string applicationUserId);

    /// <summary>
    /// Atende o pedido de exclusão (LGPD art. 18, VI) por ANONIMIZAÇÃO — e não por DELETE.
    ///
    /// POR QUE NÃO APAGAR TUDO: pedido e nota fiscal têm guarda obrigatória (5 anos, Código
    /// Tributário). A própria LGPD prevê isso no art. 16, I: o dado pode ser conservado para
    /// "cumprimento de obrigação legal". A saída é o art. 12: dado ANONIMIZADO deixa de ser
    /// dado pessoal — o registro contábil sobrevive sem apontar para uma pessoa.
    /// </summary>
    Task<ResultadoAnonimizacao> AnonimizarAsync(string applicationUserId);
}

/// <summary>Relatório do que a anonimização fez — mostrado ao titular e gravado como prova.</summary>
public record ResultadoAnonimizacao(
    int EnderecosApagados,
    int PedidosMantidos,
    int NotasMantidas,
    int EnderecosDeEntregaLimpos,
    IReadOnlyList<string> Acoes);

// ---------------------------------------------------------------------------
// O PACOTE DE EXPORTAÇÃO
//
// São records de LEITURA (DTOs), não entidades. Motivo: o que vai para o titular
// é uma FOTOGRAFIA legível — sem FKs internas, sem campos de controle, sem
// referências circulares (Cliente → Pedido → Cliente → ...) que fariam o
// serializador de JSON entrar em loop infinito.
// ---------------------------------------------------------------------------

public record DadosPessoaisExportados(
    DateTime GeradoEm,
    string Aviso,
    ClienteExportado? Cliente,
    IReadOnlyList<EnderecoExportado> Enderecos,
    IReadOnlyList<PedidoExportado> Pedidos,
    IReadOnlyList<AluguelExportado> Alugueis,
    IReadOnlyList<TrocaExportada> Trocas,
    IReadOnlyList<PropostaExportada> PropostasDeVenda);

public record ClienteExportado(
    string Nome, string? Telefone, string? CpfCnpj, string TipoPessoa, DateTime DataCadastro);

public record EnderecoExportado(
    string Cep, string Logradouro, string Numero, string? Complemento,
    string Bairro, string Cidade, string Uf, bool Principal);

public record PedidoExportado(
    int Numero, DateTime Data, string Status, decimal Total,
    decimal Frete, decimal Desconto, string? Cupom, string? FormaPagamento,
    DateTime? PagoEm, string? EnderecoEntrega, string? NotaFiscal,
    IReadOnlyList<ItemExportado> Itens);

public record ItemExportado(string Jogo, int Quantidade, decimal PrecoUnitario);

public record AluguelExportado(
    int Numero, string Jogo, DateTime Inicio, DateTime PrevisaoDevolucao,
    DateTime? DevolvidoEm, decimal Valor, string Status);

public record TrocaExportada(
    int Numero, string JogoOferecido, string JogoDesejado, DateTime Data, string Status);

public record PropostaExportada(
    int Numero, string Jogo, string Condicao, decimal ValorPedido,
    decimal? ValorAprovado, string Status, DateTime Data);
