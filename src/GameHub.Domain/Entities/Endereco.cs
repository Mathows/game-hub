using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Endereço CADASTRAL do cliente — a "agenda" de endereços. Um cliente tem VÁRIOS.
/// A chave estrangeira <see cref="ClienteId"/> mora AQUI, no lado "muitos" — que é o
/// lado correto de um relacionamento 1:N (ver Sistema.md §5.1: a regra do endereço).
/// </summary>
public class Endereco : IAuditavel
{
    public int Id { get; set; }

    // FK no lado "muitos": Cliente (1) → Endereco (N).
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    /// <summary>Marca o endereço "favorito" da agenda (o sugerido no checkout).</summary>
    public bool Principal { get; set; }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
