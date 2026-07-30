namespace GameHub.Domain.Enums;

/// <summary>
/// Ciclo de vida da NF-e (espelha o fluxo real da SEFAZ, herdado do FinFix):
///   Pendente ──→ Autorizada ──→ Cancelada
///        └─────→ Rejeitada (corrige os dados e reemite)
/// </summary>
public enum StatusNotaFiscal
{
    Pendente = 1,     // criada, aguardando resposta do emissor/SEFAZ
    Autorizada = 2,   // emitida com sucesso (tem chave de acesso válida)
    Rejeitada = 3,    // o emissor recusou (ex.: destinatário sem CPF) — pode reemitir
    Cancelada = 4     // autorizada e depois cancelada (fluxo futuro)
}
