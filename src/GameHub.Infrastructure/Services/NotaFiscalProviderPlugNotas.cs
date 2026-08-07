using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Provider REAL contra o SANDBOX público do PlugNotas (Tecnospeed) — a MESMA API fiscal
/// que o FinFix/PedidoOnline usam em produção. O sandbox é um mock público (token fixo da
/// documentação, sem cadastro, sem custo): nada vai à SEFAZ, mas o CONTRATO é o real:
///   1) POST /nfe  → aceita a nota e devolve id + protocolo ("em processamento", assíncrono)
///   2) GET /nfe/{id}/resumo → status CONCLUIDO com CHAVE, número, protocolo, XML e DANFE
/// Trocar Simulado ↔ PlugNotas = 1 linha na DI. As telas e o emissor não mudam.
/// </summary>
public class NotaFiscalProviderPlugNotas : INotaFiscalProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<NotaFiscalProviderPlugNotas> _log;

    // CNPJ de emitente de TESTE do sandbox (dados fictícios do ambiente).
    private const string CnpjEmitenteSandbox = "08187168000160";

    public NotaFiscalProviderPlugNotas(HttpClient http, ILogger<NotaFiscalProviderPlugNotas> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<ResultadoEmissao> EmitirAsync(NotaFiscal nota, Pedido pedido)
    {
        // Pré-validação local (a API real também rejeitaria): destinatário precisa de CPF/CNPJ.
        var cpfCnpj = pedido.Cliente?.CpfCnpj;
        if (string.IsNullOrWhiteSpace(cpfCnpj))
            return new ResultadoEmissao(false, null, null,
                "Destinatário sem CPF/CNPJ. Informe seus dados fiscais no perfil e reemita.");

        // idIntegracao é a NOSSA referência (única por tentativa — reemissão gera outra).
        var idIntegracao = $"GH-{pedido.Id}-{DateTime.Now:yyyyMMddHHmmss}";
        var endereco = pedido.EnderecoEntrega;

        // ---- RATEIO de frete e desconto pelos ITENS (regra da NF-e) ----
        // Na nota, frete e desconto NÃO são "itens": cada item tem seu vFrete/vDesc, e o
        // total da nota = Σ itens + frete − desconto. Isso PRECISA fechar com o pagamento,
        // senão a SEFAZ/emissor rejeita (foi o que aprendemos com o erro do 'valorTroco').
        var itensPedido = pedido.Itens.ToList();
        var subtotais = itensPedido.Select(i => i.PrecoUnitario * i.Quantidade).ToList();
        var fretePorItem = Ratear(pedido.ValorFrete, subtotais);
        var descontoPorItem = Ratear(pedido.Desconto, subtotais);

        var payload = new[]
        {
            new
            {
                idIntegracao,
                presencial = false,
                consumidorFinal = true,
                natureza = "VENDA",
                emitente = new { cpfCnpj = CnpjEmitenteSandbox },
                destinatario = new
                {
                    cpfCnpj,
                    razaoSocial = pedido.Cliente?.Nome ?? "Consumidor",
                    email = pedido.Cliente?.Nome,          // dívida conhecida: Nome guarda o e-mail
                    endereco = new
                    {
                        logradouro = endereco?.Logradouro ?? "Nao informado",
                        numero = endereco?.Numero ?? "S/N",
                        bairro = endereco?.Bairro ?? "Nao informado",
                        // Código IBGE do município: não temos ainda (o ViaCEP devolve — melhoria futura).
                        // O mock não valida; numa integração real, resolver via ViaCEP (campo "ibge").
                        codigoCidade = "3549904",
                        descricaoCidade = endereco?.Cidade ?? "Sao Jose dos Campos",
                        estado = endereco?.Uf ?? "SP",
                        cep = endereco?.Cep ?? "12223180"
                    }
                },
                itens = itensPedido.Select((i, indice) => new
                {
                    codigo = i.JogoId.ToString(),
                    descricao = i.Jogo?.Titulo ?? $"Jogo {i.JogoId}",
                    ncm = "85234090",                      // NCM de mídias gravadas (jogos)
                    cfop = "5102",                         // venda de mercadoria adquirida
                    valorUnitario = new { comercial = i.PrecoUnitario, tributavel = i.PrecoUnitario },
                    quantidade = new { comercial = i.Quantidade, tributavel = i.Quantidade },
                    valorFrete = fretePorItem[indice],       // vFrete (soma no total da nota)
                    valorDesconto = descontoPorItem[indice], // vDesc (subtrai do total)
                    tributos = new
                    {
                        icms = new { origem = "0", cst = "102" },   // Simples Nacional
                        pis = new { cst = "07" },                    // isento
                        cofins = new { cst = "07" }
                    }
                }).ToArray(),
                pagamentos = new[] { new { aVista = true, meio = "01", valor = pedido.ValorTotal } }
            }
        };

        // 1) Envia a nota.
        var resposta = await _http.PostAsJsonAsync("nfe", payload);
        var corpo = await resposta.Content.ReadAsStringAsync();
        if (!resposta.IsSuccessStatusCode)
        {
            _log.LogWarning("PlugNotas recusou o envio ({Status}): {Corpo}", resposta.StatusCode, corpo);
            var msg = ExtrairMensagemErro(corpo);
            return new ResultadoEmissao(false, null, null, $"Emissor recusou o envio: {msg}");
        }

        var envio = JsonSerializer.Deserialize<RespostaEnvio>(corpo);
        var idNota = envio?.Documents?.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(idNota))
            return new ResultadoEmissao(false, null, null, "Emissor não devolveu o id da nota.");

        // 2) Emissão é ASSÍNCRONA (como a SEFAZ real): consulta o resumo até concluir.
        for (var tentativa = 1; tentativa <= 3; tentativa++)
        {
            var resumoResp = await _http.GetAsync($"nfe/{idNota}/resumo");
            if (resumoResp.IsSuccessStatusCode)
            {
                var resumos = await resumoResp.Content.ReadFromJsonAsync<List<ResumoNota>>();
                var resumo = resumos?.FirstOrDefault();

                if (resumo?.Status == "CONCLUIDO" && !string.IsNullOrEmpty(resumo.Chave))
                {
                    _log.LogInformation("PlugNotas autorizou: chave {Chave}, nº {Numero}", resumo.Chave, resumo.Numero);
                    return new ResultadoEmissao(
                        Autorizada: true,
                        ChaveAcesso: resumo.Chave,
                        Protocolo: $"{resumo.Protocolo} (PlugNotas nº {resumo.Numero}/{resumo.Serie})",
                        MotivoRejeicao: null,
                        UrlXml: resumo.Xml,      // o XML é o documento fiscal oficial
                        UrlPdf: resumo.Pdf);     // a DANFE em PDF
                }
                if (resumo?.Status == "REJEITADO")
                    return new ResultadoEmissao(false, null, null,
                        $"Rejeitada pelo emissor: {resumo.Mensagem ?? "sem detalhe"}");
            }
            await Task.Delay(1000);   // ainda processando → espera 1s e tenta de novo
        }

        return new ResultadoEmissao(false, null, null,
            "A nota ainda está em processamento no emissor — tente reemitir em instantes.");
    }

    /// <summary>
    /// Rateia um valor (frete/desconto) entre os itens, proporcional ao subtotal de cada um.
    /// O ÚLTIMO item recebe a diferença de arredondamento — assim a soma rateada é EXATAMENTE
    /// o valor original (centavo por centavo). Sem isso, o total da nota não fecharia com o
    /// pagamento e o emissor rejeitaria.
    /// </summary>
    private static List<decimal> Ratear(decimal valor, List<decimal> pesos)
    {
        var rateio = pesos.Select(_ => 0m).ToList();
        if (valor <= 0 || pesos.Count == 0) return rateio;

        var totalPesos = pesos.Sum();
        if (totalPesos <= 0)                       // sem base de rateio: tudo no primeiro item
        {
            rateio[0] = valor;
            return rateio;
        }

        var acumulado = 0m;
        for (var i = 0; i < pesos.Count - 1; i++)
        {
            rateio[i] = Math.Round(valor * (pesos[i] / totalPesos), 2);
            acumulado += rateio[i];
        }
        rateio[^1] = valor - acumulado;            // o resto vai no último (fecha a conta)
        return rateio;
    }

    private static string ExtrairMensagemErro(string corpo)
    {
        try
        {
            using var doc = JsonDocument.Parse(corpo);
            if (doc.RootElement.TryGetProperty("message", out var m)) return m.ToString();
        }
        catch { /* corpo não é JSON — devolve cru */ }
        return corpo.Length > 200 ? corpo[..200] : corpo;
    }

    // ---- Formato das respostas do PlugNotas (só o que usamos) ----
    private sealed class RespostaEnvio
    {
        [JsonPropertyName("documents")] public List<DocumentoEnvio>? Documents { get; set; }
        [JsonPropertyName("protocol")] public string? Protocol { get; set; }
    }
    private sealed class DocumentoEnvio
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
    private sealed class ResumoNota
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("chave")] public string? Chave { get; set; }
        [JsonPropertyName("numero")] public string? Numero { get; set; }
        [JsonPropertyName("serie")] public string? Serie { get; set; }
        [JsonPropertyName("protocolo")] public string? Protocolo { get; set; }
        [JsonPropertyName("mensagem")] public string? Mensagem { get; set; }
        [JsonPropertyName("xml")] public string? Xml { get; set; }
        [JsonPropertyName("pdf")] public string? Pdf { get; set; }
    }
}
