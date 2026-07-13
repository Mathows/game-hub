using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Agenda de endereços (EF Core, Scoped). Mesmo padrão do PedidoService: get-or-create do
/// Cliente pelo ApplicationUserId. A FK ClienteId mora na Endereco (Cliente 1 → N Endereco).
/// </summary>
public class EnderecoService : IEnderecoService
{
    private readonly GameHubDbContext _context;

    public EnderecoService(GameHubDbContext context) => _context = context;

    public async Task<List<Endereco>> ListarDoUsuarioAsync(string applicationUserId)
        => await _context.Enderecos
            .AsNoTracking()                                            // leitura pura, dados frescos
            .Where(e => e.Cliente!.ApplicationUserId == applicationUserId)
            .OrderByDescending(e => e.Principal)                       // o "principal" primeiro
            .ThenBy(e => e.Id)
            .ToListAsync();

    public async Task AdicionarAsync(string applicationUserId, string nomeCliente, Endereco endereco)
    {
        // Get-or-create do Cliente (ponte login ↔ loja).
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.ApplicationUserId == applicationUserId);

        if (cliente is null)
        {
            cliente = new Cliente { Nome = nomeCliente, ApplicationUserId = applicationUserId };
            _context.Clientes.Add(cliente);
        }

        // Se este for o primeiro endereço do cliente, marca como principal automaticamente.
        var jaTemEndereco = cliente.Id != 0 &&
            await _context.Enderecos.AnyAsync(e => e.ClienteId == cliente.Id);
        if (!jaTemEndereco) endereco.Principal = true;

        endereco.Cliente = cliente;   // liga à agenda (o EF preenche o ClienteId)
        _context.Enderecos.Add(endereco);

        await _context.SaveChangesAsync();
    }
}
