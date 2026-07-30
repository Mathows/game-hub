using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;
using GameHub.Domain.Interfaces;
using GameHub.Domain.Services;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>Dados fiscais do cliente (EF Core, Scoped). Guarda o CPF/CNPJ SÓ COM DÍGITOS.</summary>
public class ClienteService : IClienteService
{
    private readonly GameHubDbContext _context;

    public ClienteService(GameHubDbContext context) => _context = context;

    public async Task<Cliente?> ObterAsync(string applicationUserId)
        => await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

    public async Task SalvarDadosFiscaisAsync(string applicationUserId, string nomeCliente,
        TipoPessoa tipoPessoa, string cpfCnpj, string? telefone)
    {
        // A validação QUE VALE (servidor): dígitos verificadores conforme o tipo.
        var digitos = ValidadorCpfCnpj.SoDigitos(cpfCnpj);
        var valido = tipoPessoa == TipoPessoa.Fisica
            ? ValidadorCpfCnpj.ValidarCpf(digitos)
            : ValidadorCpfCnpj.ValidarCnpj(digitos);
        if (!valido)
            throw new InvalidOperationException(tipoPessoa == TipoPessoa.Fisica
                ? "CPF inválido — confira os dígitos."
                : "CNPJ inválido — confira os dígitos.");

        // Get-or-create do Cliente (a ponte login ↔ loja, padrão do projeto).
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);
        if (cliente is null)
        {
            cliente = new Cliente { Nome = nomeCliente, ApplicationUserId = applicationUserId };
            _context.Clientes.Add(cliente);
        }

        cliente.TipoPessoa = tipoPessoa;
        cliente.CpfCnpj = digitos;                    // só dígitos no banco; máscara é exibição
        if (!string.IsNullOrWhiteSpace(telefone))
            cliente.Telefone = telefone.Trim();

        await _context.SaveChangesAsync();            // auditoria automática carimba a alteração
    }
}
