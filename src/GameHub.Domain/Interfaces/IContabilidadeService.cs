using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>Resultado do fechamento: o ZIP pronto + as estatísticas (transparência do que entrou).</summary>
public record ResultadoPacote(
    byte[] Zip,
    string NomeArquivo,
    int TotalNotas,
    int Autorizadas,
    int Rejeitadas,
    int XmlsIncluidos,
    int XmlsNaoEncontrados);   // notas sem arquivo (ex.: emitidas pelo provider simulado)

/// <summary>
/// Fechamento mensal para a CONTABILIDADE (herdado do FinFix, reconstruído):
/// CSV índice com VERIFICAÇÃO DE SEQUÊNCIA da numeração + XMLs das notas autorizadas,
/// tudo num ZIP. O XML é o documento fiscal oficial — é o que o contador escritura.
/// </summary>
public interface IContabilidadeService
{
    /// <summary>Notas do período (para a tela mostrar antes de gerar).</summary>
    Task<List<NotaFiscal>> ListarPeriodoAsync(int ano, int mes);

    /// <summary>Gera o pacote do mês (ou null se não há notas no período).</summary>
    Task<ResultadoPacote?> GerarPacoteAsync(int ano, int mes);
}
