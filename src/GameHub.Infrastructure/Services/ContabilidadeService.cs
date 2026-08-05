using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Fechamento mensal da contabilidade (o fluxo do FinFix, modernizado):
/// 1) CSV índice VARRENDO A SEQUÊNCIA numérica (número sem nota autorizada = linha
///    "INUTILIZAR" — a numeração de NF-e não pode ter buracos sem justificativa);
/// 2) baixa os XMLs das autorizadas no emissor (com o token, que fica no servidor);
/// 3) zipa tudo com o ZipArchive NATIVO (System.IO.Compression — sem lib externa,
///    diferente do Ionic.Zip do legado).
/// Tudo EM MEMÓRIA: nada de arquivos temporários em disco como o legado fazia.
/// </summary>
public class ContabilidadeService : IContabilidadeService
{
    private readonly GameHubDbContext _context;
    private readonly HttpClient _http;   // já vem com o x-api-key do PlugNotas (typed client)

    public ContabilidadeService(GameHubDbContext context, HttpClient http)
    {
        _context = context;
        _http = http;
    }

    public async Task<List<NotaFiscal>> ListarPeriodoAsync(int ano, int mes)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fim = inicio.AddMonths(1);
        return await _context.NotasFiscais.AsNoTracking()
            .Include(n => n.Pedido!).ThenInclude(p => p.Cliente)
            .Where(n => n.DataEmissao >= inicio && n.DataEmissao < fim)
            .OrderBy(n => n.Numero)
            .ToListAsync();
    }

    public async Task<ResultadoPacote?> GerarPacoteAsync(int ano, int mes)
    {
        var notas = await ListarPeriodoAsync(ano, mes);
        if (notas.Count == 0) return null;

        // ---- 1) CSV índice com VERIFICAÇÃO DE SEQUÊNCIA (a lição do FinFix) ----
        var csv = new StringBuilder();
        csv.AppendLine("NumeroNota;Serie;Cliente;Pedido;Status;DataEmissao;ValorTotal;Chave");

        var numeroInicial = notas.Min(n => n.Numero);
        var numeroFinal = notas.Max(n => n.Numero);
        for (var numero = numeroInicial; numero <= numeroFinal; numero++)
        {
            var nota = notas.FirstOrDefault(n => n.Numero == numero);
            if (nota is null)
            {
                // Buraco na sequência: precisa ser justificado (inutilização) — o CSV denuncia.
                csv.AppendLine($"{numero};1;NAO ENCONTRADO;;INUTILIZAR;;0;");
            }
            else
            {
                csv.AppendLine(string.Join(";",
                    nota.Numero,
                    nota.Serie,
                    nota.Pedido?.Cliente?.Nome,
                    nota.PedidoId,
                    nota.Status,
                    nota.DataEmissao.ToString("dd/MM/yyyy HH:mm"),
                    nota.ValorTotal.ToString("F2"),
                    nota.ChaveAcesso));
            }
        }

        // ---- 2 + 3) XMLs das autorizadas + ZIP em memória ----
        var xmlsIncluidos = 0;
        var xmlsNaoEncontrados = 0;

        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            // O CSV índice entra primeiro.
            var entradaCsv = zip.CreateEntry("Pacote.csv");
            await using (var stream = entradaCsv.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                await stream.WriteAsync(bytes);
            }

            // Um XML por nota AUTORIZADA (o documento oficial). Sem URL (provider simulado
            // ou nota antiga) = "arquivo não encontrado", contado — como o legado fazia.
            foreach (var nota in notas.Where(n => n.Status == StatusNotaFiscal.Autorizada))
            {
                if (string.IsNullOrEmpty(nota.UrlXml)) { xmlsNaoEncontrados++; continue; }
                try
                {
                    var xml = await _http.GetByteArrayAsync(nota.UrlXml);
                    var entrada = zip.CreateEntry($"{nota.Numero:D9}.xml");
                    await using var stream = entrada.Open();
                    await stream.WriteAsync(xml);
                    xmlsIncluidos++;
                }
                catch
                {
                    xmlsNaoEncontrados++;   // emissor fora do ar/arquivo sumiu: conta e segue
                }
            }
        }

        return new ResultadoPacote(
            Zip: memoria.ToArray(),
            NomeArquivo: $"Contabilidade_GameHub_{ano}-{mes:D2}.zip",
            TotalNotas: notas.Count,
            Autorizadas: notas.Count(n => n.Status == StatusNotaFiscal.Autorizada),
            Rejeitadas: notas.Count(n => n.Status == StatusNotaFiscal.Rejeitada),
            XmlsIncluidos: xmlsIncluidos,
            XmlsNaoEncontrados: xmlsNaoEncontrados);
    }
}
