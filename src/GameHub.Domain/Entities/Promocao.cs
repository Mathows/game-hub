using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Promoção de um jogo: durante a vigência (Início→Fim), o jogo é vendido pelo
/// <see cref="PrecoPromocional"/> em vez do preço normal.
///
/// É uma ENTIDADE própria (não um campo no Jogo) de propósito:
/// - um jogo pode ter VÁRIAS promoções ao longo do tempo (histórico comercial);
/// - e ela é auditável (quem criou a promoção? quando?) — herança da Fase 6.
/// A regra de "qual preço vale agora" mora no Jogo (PrecoVigente).
/// </summary>
public class Promocao : IAuditavel
{
    public int Id { get; set; }

    // FK no lado "muitos": Jogo (1) → Promocao (N).
    public int JogoId { get; set; }
    public Jogo? Jogo { get; set; }

    /// <summary>Nome da campanha (ex.: "Black Friday", "Semana do Consumidor").</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Preço de venda DURANTE a promoção (deve ser menor que o preço normal).</summary>
    public decimal PrecoPromocional { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }

    /// <summary>Interruptor manual: permite desligar antes do fim sem apagar (preserva histórico).</summary>
    public bool Ativa { get; set; } = true;

    /// <summary>A promoção vale NESTE momento? (ligada + dentro da vigência)</summary>
    public bool VigenteEm(DateTime momento) => Ativa && Inicio <= momento && momento <= Fim;

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
