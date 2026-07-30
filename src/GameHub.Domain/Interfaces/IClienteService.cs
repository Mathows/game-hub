using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Domain.Interfaces;

/// <summary>Dados fiscais do cliente (CPF/CNPJ) — o que a nota fiscal exige do destinatário.</summary>
public interface IClienteService
{
    /// <summary>O cadastro do cliente ligado a este login (ou null se ainda não existe).</summary>
    Task<Cliente?> ObterAsync(string applicationUserId);

    /// <summary>
    /// Salva os dados fiscais (get-or-create do Cliente). VALIDA o CPF/CNPJ pelos dígitos
    /// verificadores NO SERVIDOR — máscara e aviso na tela são cortesia; a regra mora aqui.
    /// </summary>
    Task SalvarDadosFiscaisAsync(string applicationUserId, string nomeCliente,
        TipoPessoa tipoPessoa, string cpfCnpj, string? telefone);
}
