using GameHub.Domain.Entities;

namespace GameHub.Web.Services;

/// <summary>Se o item do carrinho é para COMPRAR ou para ALUGAR.</summary>
public enum TipoAquisicao
{
    Compra,
    Aluguel
}

/// <summary>
/// Uma linha do carrinho. Guardamos um "retrato" (snapshot) do jogo — título, foto e preço —
/// em vez do objeto Jogo inteiro. Motivo: o preço fica CONGELADO no momento em que o cliente
/// adicionou (se a loja mudar o preço depois, o carrinho respeita o que ele viu), e evitamos
/// segurar uma entidade que pertence a um DbContext já descartado.
/// </summary>
public class ItemCarrinho
{
    public int JogoId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? UrlFoto { get; init; }

    public TipoAquisicao Tipo { get; init; }
    public int Quantidade { get; set; } = 1;
    public int DiasAluguel { get; set; }          // usado só quando Tipo == Aluguel

    public decimal PrecoUnitario { get; init; }   // preço de venda OU preço/dia, congelado

    // Compra: preço × quantidade. Aluguel: preço/dia × dias.
    public decimal Subtotal => Tipo == TipoAquisicao.Compra
        ? PrecoUnitario * Quantidade
        : PrecoUnitario * DiasAluguel;
}

/// <summary>
/// Carrinho de compras de UM usuário. É registrado como <b>Scoped</b> no Program.cs:
/// no Blazor Server, "Scoped" = uma instância por <b>circuito</b> (a conexão SignalR do usuário).
/// Ou seja, cada usuário tem o SEU carrinho, e ele vive enquanto o usuário está conectado.
///
/// Como o estado (a lista) é do usuário, este serviço NÃO pode ser Singleton (misturaria o
/// carrinho de todo mundo) nem Transient (cada componente veria um carrinho vazio diferente).
///
/// Repare também que este serviço mora no projeto Web, não em Infrastructure: ele é estado de
/// tela/sessão do usuário, não acesso a banco de dados. Nem todo serviço vai para Infrastructure.
/// </summary>
public class CarrinhoService
{
    private readonly List<ItemCarrinho> _itens = new();

    // Só de leitura para fora: quem quiser mexer usa os métodos abaixo.
    public IReadOnlyList<ItemCarrinho> Itens => _itens;

    public int TotalItens => _itens.Sum(i => i.Quantidade);
    public decimal ValorTotal => _itens.Sum(i => i.Subtotal);

    /// <summary>
    /// Avisa quem estiver "ouvindo" (o selo do cabeçalho, a página do carrinho) que algo mudou.
    /// É assim que dois componentes diferentes, olhando a MESMA instância Scoped, se atualizam juntos.
    /// </summary>
    public event Action? OnChange;

    public void AdicionarCompra(Jogo jogo, int quantidade = 1)
    {
        // Se o mesmo jogo já está no carrinho para compra, só aumenta a quantidade.
        var existente = _itens.FirstOrDefault(i => i.JogoId == jogo.Id && i.Tipo == TipoAquisicao.Compra);
        if (existente is not null)
        {
            existente.Quantidade += quantidade;
        }
        else
        {
            _itens.Add(new ItemCarrinho
            {
                JogoId = jogo.Id,
                Titulo = jogo.Titulo,
                UrlFoto = jogo.UrlFoto,
                Tipo = TipoAquisicao.Compra,
                Quantidade = quantidade,
                PrecoUnitario = jogo.PrecoVenda
            });
        }
        NotificarMudanca();
    }

    public void AdicionarAluguel(Jogo jogo, int dias)
    {
        if (dias < 1) dias = 1;

        _itens.Add(new ItemCarrinho
        {
            JogoId = jogo.Id,
            Titulo = jogo.Titulo,
            UrlFoto = jogo.UrlFoto,
            Tipo = TipoAquisicao.Aluguel,
            DiasAluguel = dias,
            PrecoUnitario = jogo.PrecoAluguelDia
        });
        NotificarMudanca();
    }

    public void Remover(ItemCarrinho item)
    {
        _itens.Remove(item);
        NotificarMudanca();
    }

    public void Limpar()
    {
        _itens.Clear();
        NotificarMudanca();
    }

    private void NotificarMudanca() => OnChange?.Invoke();
}
