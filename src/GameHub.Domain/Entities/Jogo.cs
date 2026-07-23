using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>Um jogo do catálogo da loja — o item central que se vende, aluga e troca.</summary>
public class Jogo : IAuditavel
{
    public int Id { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }          // o "?" indica que pode ficar vazio (nulo)

    public CondicaoJogo Condicao { get; set; } = CondicaoJogo.Usado;

    public decimal PrecoVenda { get; set; }         // preço para COMPRAR
    public decimal PrecoAluguelDia { get; set; }    // preço por DIA de aluguel

    public int QuantidadeEstoque { get; set; }
    public bool Disponivel { get; set; } = true;

    public string? UrlFoto { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // --- Relacionamentos ---
    // FK (número gravado na tabela) + navegação (objeto para usar no código)
    public int PlataformaId { get; set; }
    public Plataforma? Plataforma { get; set; }

    public int GeneroId { get; set; }
    public Genero? Genero { get; set; }

    // Um jogo pode ter várias promoções ao longo do tempo (histórico). FK na Promocao.
    public ICollection<Promocao> Promocoes { get; set; } = new List<Promocao>();

    /// <summary>A promoção que vale NESTE momento (ou null). Se houver mais de uma vigente,
    /// vence a de menor preço (a mais vantajosa pro cliente).</summary>
    public Promocao? PromocaoVigente(DateTime momento) =>
        Promocoes.Where(p => p.VigenteEm(momento) && p.PrecoPromocional < PrecoVenda)
                 .OrderBy(p => p.PrecoPromocional)
                 .FirstOrDefault();

    /// <summary>
    /// REGRA DE NEGÓCIO central de preço: o preço que vale AGORA (promocional se houver
    /// promoção vigente; senão o normal). Mora no Domain e é usada pelo servidor — a tela
    /// só EXIBE; quem decide o preço cobrado é sempre o servidor (nunca confiar no cliente).
    /// Atenção: exige as Promocoes carregadas (Include) — senão enxerga só o preço normal.
    /// </summary>
    public decimal PrecoVigente(DateTime momento) =>
        PromocaoVigente(momento)?.PrecoPromocional ?? PrecoVenda;

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
