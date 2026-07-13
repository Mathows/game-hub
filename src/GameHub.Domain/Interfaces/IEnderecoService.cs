using GameHub.Domain.Entities;

namespace GameHub.Domain.Interfaces;

/// <summary>Gerencia a agenda de endereços do cliente (listar e adicionar).</summary>
public interface IEnderecoService
{
    /// <summary>Lista os endereços do cliente ligado a este usuário de login.</summary>
    Task<List<Endereco>> ListarDoUsuarioAsync(string applicationUserId);

    /// <summary>
    /// Adiciona um endereço à agenda do cliente. Se o cliente ainda não existe (1º cadastro),
    /// cria (get-or-create pelo ApplicationUserId — a ponte login ↔ loja).
    /// </summary>
    Task AdicionarAsync(string applicationUserId, string nomeCliente, Endereco endereco);
}
