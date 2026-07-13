namespace GameHub.Domain.Entities;

/// <summary>
/// Endereço de ENTREGA de um pedido — um SNAPSHOT (cópia imutável) tirado no checkout.
///
/// É um <b>Value Object</b>: não tem Id próprio, ele "pertence" ao Pedido. No EF Core vira
/// um <i>owned type</i> — as colunas (EnderecoEntrega_Cep, _Logradouro, …) ficam DENTRO da
/// própria tabela Pedido. Assim "1 pedido = 1 endereço" é garantido pelo schema, não por
/// regra no código (ver Sistema.md §5.1).
///
/// Copiar (snapshot) em vez de referenciar a agenda preserva o histórico: se o cliente
/// editar ou apagar o endereço da agenda depois, os pedidos antigos NÃO mudam.
///
/// Obs.: é um POCO puro (sem atributo [Owned]) para o Domain não depender do EF — a
/// configuração de "owned" fica no GameHubDbContext (OnModelCreating).
/// </summary>
public class EnderecoEntrega
{
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
}
