using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;

namespace GameHub.Domain.Entities;

/// <summary>
/// Cliente da loja. Fica LIGADO ao usuário de login (ASP.NET Identity)
/// através do Id do usuário — é a "ponte" entre a loja e o sistema de login.
/// </summary>
public class Cliente : IAuditavel
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Telefone { get; set; }

    // ---- Dados fiscais (pré-requisito de nota fiscal / boleto lá na frente) ----
    /// <summary>CPF (pessoa física) ou CNPJ (pessoa jurídica). Só dígitos.</summary>
    public string? CpfCnpj { get; set; }
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Fisica;

    /// <summary>Id do usuário na tabela AspNetUsers (login). Liga o cliente à conta.</summary>
    public string? ApplicationUserId { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    // ---- LGPD (Fase 11) ----
    /// <summary>
    /// Marca que este cadastro foi ANONIMIZADO a pedido do titular.
    ///
    /// Cuidado com a leitura: esta flag NÃO é o que protege o dado — quem protege é a
    /// anonimização em si (nome, CPF e telefone sobrescritos). A flag existe para o
    /// SISTEMA saber que "Titular removido #7" é uma exclusão legítima, e não um cadastro
    /// corrompido: assim ele não aparece em listagens de clientes, não recebe e-mail de
    /// marketing e não entra em relatório de base ativa.
    ///
    /// Ou seja: inativar (flag) e anonimizar (esvaziar o dado) resolvem problemas
    /// diferentes — e aqui os dois andam juntos.
    /// </summary>
    public bool Anonimizado { get; set; }

    public DateTime? AnonimizadoEm { get; set; }

    // ---- Agenda de endereços: Cliente (1) → Endereco (N). A FK mora na Endereco. ----
    // (Substitui o antigo "string? Endereco" ingênuo — ver Sistema.md §5.1.)
    public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();

    // Um cliente tem vários pedidos, aluguéis e trocas (navegações um-para-muitos).
    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
    public ICollection<Troca> Trocas { get; set; } = new List<Troca>();

    // --- Auditoria (preenchida automaticamente pelo AuditoriaInterceptor) ---
    public DateTime CriadoEm { get; set; }
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
}
