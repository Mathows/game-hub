using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Parâmetros globais do frete — uma ÚNICA linha no banco (padrão "singleton row",
/// comum para configuração de sistema). Editável pelo admin, sem deploy.
/// </summary>
public class ConfiguracaoFrete : IAuditavel
{
    public int Id { get; set; }

    /// <summary>Valor base, antes do fator da região.</summary>
    public decimal ValorBase { get; set; } = 12.90m;

    /// <summary>Adicional por item no carrinho.</summary>
    public decimal ValorPorItem { get; set; } = 2.50m;

    /// <summary>Prazo base em dias úteis (a região soma os dias extras).</summary>
    public int PrazoBaseDias { get; set; } = 2;

    /// <summary>Compras a partir deste valor têm FRETE GRÁTIS (0 = promoção desligada).</summary>
    public decimal FreteGratisAcimaDe { get; set; }

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
