using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Interfaces;
using GameHub.Domain.Services;
using GameHub.Infrastructure.Data;

namespace GameHub.Infrastructure.Services;

/// <summary>
/// Frete calculado por TABELA PRÓPRIA (Scoped, usa o DbContext).
///
/// A divisão de responsabilidade é o ponto didático (Sistema.md §5.2):
///   • a FÓRMULA fica no código (regra de negócio estável):
///         valor = (ValorBase × FatorDaRegiao) + (ValorPorItem × itens)
///         prazo = PrazoBaseDias + DiasExtrasDaRegiao
///   • os VALORES ficam no banco (dado comercial editável pelo admin, sem deploy).
///
/// A região vem do 1º dígito do CEP — regra real dos Correios.
/// </summary>
public class FreteTabelaService : IFreteService
{
    private readonly GameHubDbContext _context;

    public FreteTabelaService(GameHubDbContext context) => _context = context;

    public async Task<ResultadoFrete> CalcularAsync(string cepDestino, int quantidadeItens, decimal subtotalCompras = 0)
    {
        var digitos = ValidadorCpfCnpj.SoDigitos(cepDestino);   // reusa o "só dígitos"
        if (digitos.Length != 8)
            throw new InvalidOperationException("CEP de destino inválido para o cálculo do frete.");
        if (quantidadeItens < 1) quantidadeItens = 1;

        var digito = digitos[0] - '0';
        var regiao = await _context.RegioesFrete.AsNoTracking()
            .FirstOrDefaultAsync(r => r.DigitoCep == digito)
            ?? throw new InvalidOperationException("Não há tabela de frete para essa região.");

        if (!regiao.Ativo)
            throw new InvalidOperationException($"Ainda não entregamos em {regiao.Nome}.");

        // Configuração global (linha única). Se faltar, usa os defaults da entidade.
        var config = await _context.ConfiguracoesFrete.AsNoTracking().FirstOrDefaultAsync()
            ?? new Domain.Entities.ConfiguracaoFrete();

        var prazo = config.PrazoBaseDias + regiao.DiasExtras;

        // FRETE GRÁTIS: promoção por valor de compra (0 = desligada).
        if (config.FreteGratisAcimaDe > 0 && subtotalCompras >= config.FreteGratisAcimaDe)
            return new ResultadoFrete(0m, prazo,
                $"Frete GRÁTIS para {regiao.Nome} (compras acima de R$ {config.FreteGratisAcimaDe:N2}) — até {prazo} dias úteis");

        var valor = Math.Round(config.ValorBase * regiao.Fator + config.ValorPorItem * quantidadeItens, 2);
        return new ResultadoFrete(valor, prazo,
            $"Envio padrão para {regiao.Nome} — até {prazo} dias úteis");
    }
}
