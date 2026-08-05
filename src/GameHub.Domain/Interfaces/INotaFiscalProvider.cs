using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>Resposta do emissor: autorizou (chave + protocolo + links dos arquivos) ou rejeitou (motivo).</summary>
public record ResultadoEmissao(bool Autorizada, string? ChaveAcesso, string? Protocolo, string? MotivoRejeicao,
    string? UrlXml = null, string? UrlPdf = null);

/// <summary>
/// O "lado SEFAZ" da emissão — PROVIDER PLUGÁVEL (Sistema.md §4.1):
/// hoje a implementação é SIMULADA (grátis); amanhã pluga o sandbox do Focus NFe/PlugNotas
/// e depois a produção — trocando só o registro na DI, sem mexer no emissor nem nas telas.
/// (O mesmo padrão do e-mail simulado→Gmail e do login Google.)
/// </summary>
public interface INotaFiscalProvider
{
    /// <summary>Tenta emitir a nota do pedido junto ao "fisco". O pedido chega com Cliente e Itens carregados.</summary>
    Task<ResultadoEmissao> EmitirAsync(NotaFiscal nota, Pedido pedido);
}
