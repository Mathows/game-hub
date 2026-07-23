namespace GameHub.Domain.Interfaces;

/// <summary>
/// Marca uma entidade como AUDITÁVEL: ela guarda quem criou/alterou e quando.
/// O preenchimento é AUTOMÁTICO (um interceptor no SaveChanges carimba tudo que
/// implementa esta interface) — ninguém precisa lembrar de setar na mão.
///
/// Guardamos o <b>Id</b> do usuário (estável e único), não o nome — nome muda e repete.
/// Na hora de exibir, resolvemos o nome pelo Id.
/// </summary>
public interface IAuditavel
{
    DateTime CriadoEm { get; set; }
    string? CriadoPor { get; set; }
    DateTime? AtualizadoEm { get; set; }
    string? AtualizadoPor { get; set; }
}
