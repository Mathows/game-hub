using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using GameHub.Domain.Interfaces;

namespace GameHub.Infrastructure.Data;

/// <summary>
/// Carimba a auditoria AUTOMATICAMENTE em toda gravação: intercepta o SaveChanges e, para
/// cada entidade que implementa <see cref="IAuditavel"/>, preenche CriadoEm/CriadoPor (ao
/// inserir) ou AtualizadoEm/AtualizadoPor (ao alterar). Ninguém precisa lembrar de setar.
///
/// O "quem" vem do <see cref="IUsuarioAtual"/> (o Id do usuário logado — estável, não o nome).
/// Depende dessa interface (não do HttpContext) para o Infrastructure não conhecer HTTP.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IUsuarioAtual _usuarioAtual;

    public AuditoriaInterceptor(IUsuarioAtual usuarioAtual) => _usuarioAtual = usuarioAtual;

    // SaveChanges tem versão síncrona e assíncrona — carimbamos nas duas.
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Carimbar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Carimbar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Carimbar(DbContext? context)
    {
        if (context is null) return;

        var usuarioId = _usuarioAtual.Id;     // Id do usuário logado (ou null se sistema/anônimo)
        var agora = DateTime.Now;

        // ChangeTracker.Entries<IAuditavel>() = só as entidades auditáveis que mudaram.
        foreach (var entry in context.ChangeTracker.Entries<IAuditavel>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
                entry.Entity.CriadoPor = usuarioId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = agora;
                entry.Entity.AtualizadoPor = usuarioId;

                // Protege os campos de criação: um UPDATE nunca reescreve quem/quando criou.
                entry.Property(nameof(IAuditavel.CriadoEm)).IsModified = false;
                entry.Property(nameof(IAuditavel.CriadoPor)).IsModified = false;
            }
        }
    }
}
