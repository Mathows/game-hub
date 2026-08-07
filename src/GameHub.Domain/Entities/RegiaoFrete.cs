using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Uma REGIÃO POSTAL na tabela de frete — indexada pelo 1º dígito do CEP (regra real dos
/// Correios: 0=SP capital, 1=interior de SP, 2=RJ/ES, ... 9=RS).
///
/// Está no BANCO (e não hardcoded) porque é **dado comercial editável**: combustível sobe,
/// a transportadora muda, o prazo piora — e quem ajusta isso é o admin, não o programador.
/// É o critério do Sistema.md §5.2 aplicado: **a fórmula fica no código; os valores, no banco.**
/// </summary>
public class RegiaoFrete : IAuditavel
{
    public int Id { get; set; }

    /// <summary>Primeiro dígito do CEP que esta linha atende (0 a 9) — único.</summary>
    public int DigitoCep { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Multiplicador do valor base (1,0 = base; 1,8 = 80% mais caro).</summary>
    public decimal Fator { get; set; } = 1m;

    /// <summary>Dias somados ao prazo base.</summary>
    public int DiasExtras { get; set; }

    /// <summary>Desativar = não entregamos nessa região (o checkout avisa).</summary>
    public bool Ativo { get; set; } = true;

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
