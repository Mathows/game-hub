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

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
