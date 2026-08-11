namespace GameHub.Domain.Entities;

/// <summary>
/// Uma VERSÃO publicada dos termos de uso / política de privacidade.
///
/// POR QUE VERSIONAR EM TABELA, e não deixar o texto solto numa página:
/// o consentimento da LGPD é sempre consentimento PARA ALGO ESPECÍFICO (art. 8º, §4º:
/// "referente a finalidades determinadas"). Se o texto muda e você só reescreve a página,
/// perde a capacidade de responder à única pergunta que importa numa fiscalização:
/// "o que exatamente esta pessoa aceitou, na data em que aceitou?".
///
/// Guardar o CONTEÚDO junto com a versão é o mesmo princípio do snapshot de preço no
/// pedido (§5.1): o documento aceito no passado não pode mudar depois.
/// </summary>
public class TermoDeUso
{
    public int Id { get; set; }

    /// <summary>Versão legível: "1.0", "1.1", "2.0".</summary>
    public string Versao { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    /// <summary>O texto na íntegra, como estava quando foi publicado (imutável).</summary>
    public string Conteudo { get; set; } = string.Empty;

    /// <summary>
    /// Resumo do que MUDOU em relação à versão anterior. É o que se mostra ao usuário
    /// quando pedimos um novo aceite — ninguém relê 4 páginas para achar a diferença.
    /// </summary>
    public string? ResumoDasMudancas { get; set; }

    public DateTime PublicadoEm { get; set; } = DateTime.Now;

    /// <summary>
    /// Versão em vigor. Só uma fica true — as antigas continuam na tabela porque há
    /// aceites apontando para elas (e é justamente esse histórico que prova o consentimento).
    /// </summary>
    public bool Vigente { get; set; }

    public ICollection<AceiteTermo> Aceites { get; set; } = new List<AceiteTermo>();
}
