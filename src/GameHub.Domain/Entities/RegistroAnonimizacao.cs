namespace GameHub.Domain.Entities;

/// <summary>
/// Prova de que um pedido de exclusão foi ATENDIDO.
///
/// Parece contraditório guardar um registro de quem pediu para ser esquecido — mas não é:
/// aqui não fica NENHUM dado pessoal. Fica o Id numérico do cliente (que continua no banco,
/// já anonimizado), a data e o que foi feito. Sem isso, a loja não tem como demonstrar
/// à ANPD que cumpriu o pedido no prazo — e o ônus da prova é de quem trata o dado.
/// </summary>
public class RegistroAnonimizacao
{
    public int Id { get; set; }

    /// <summary>Cliente afetado. O Id sobrevive; o nome, CPF e telefone não.</summary>
    public int? ClienteId { get; set; }

    public DateTime SolicitadoEm { get; set; } = DateTime.Now;

    /// <summary>O que a rotina fez, em texto — a trilha de auditoria do próprio apagamento.</summary>
    public string ResumoAcoes { get; set; } = string.Empty;

    /// <summary>Quantos pedidos foram MANTIDOS por obrigação fiscal (guarda de 5 anos).</summary>
    public int PedidosMantidos { get; set; }

    /// <summary>Quantas notas fiscais foram mantidas por obrigação fiscal.</summary>
    public int NotasMantidas { get; set; }
}
