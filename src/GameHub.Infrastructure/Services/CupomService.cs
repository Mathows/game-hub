using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Interfaces;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>Validação de cupom p/ a prévia do carrinho (Scoped, usa o DbContext).</summary>
public class CupomService : ICupomService
{
    private readonly GameHubDbContext _context;

    public CupomService(GameHubDbContext context) => _context = context;

    public async Task<ResultadoCupom> ValidarAsync(string codigo, decimal subtotal)
    {
        var codigoLimpo = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (codigoLimpo.Length == 0)
            return new(false, "Digite um código de cupom.", 0);

        var cupom = await _context.Cupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Codigo == codigoLimpo);

        if (cupom is null)
            return new(false, "Cupom não encontrado.", 0);
        if (!cupom.ValidoEm(DateTime.Now))
            return new(false, "Este cupom não está mais válido (expirado, esgotado ou desativado).", 0);

        var desconto = cupom.CalcularDesconto(subtotal);
        return new(true, $"Cupom {cupom.Codigo} aplicado: −R$ {desconto:N2}", desconto);
    }
}
