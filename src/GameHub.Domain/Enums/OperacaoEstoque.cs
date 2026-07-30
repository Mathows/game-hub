namespace GameHub.Domain.Enums;

/// <summary>
/// Direção da movimentação manual: entra ou sai. É ENUM (não tabela) porque o fluxo é
/// FIXO — nunca haverá uma terceira direção. Já o MOTIVO é tabela, porque a lista cresce
/// com o uso (critério do Sistema.md §5.2: enum p/ fluxo fixo, lookup p/ lista editável).
/// </summary>
public enum OperacaoEstoque
{
    Entrada = 1,
    Saida = 2
}
