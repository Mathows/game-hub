using Microsoft.EntityFrameworkCore;
using GameHub.Domain.Entities;
using GameHub.Domain.Enums;

namespace GameHub.Infrastructure.Data;

/// <summary>
/// Contexto de dados da LOJA (separado do contexto de login/Identity).
/// Representa o banco: cada DbSet vira uma tabela; este contexto é quem
/// lê e grava os dados de jogos, clientes, pedidos, aluguéis e trocas.
/// </summary>
public class GameHubDbContext : DbContext
{
    // O construtor recebe as "opções" (ex.: qual banco usar) via Injeção de Dependência.
    public GameHubDbContext(DbContextOptions<GameHubDbContext> options)
        : base(options)
    {
    }

    // Cada DbSet<T> representa uma TABELA no banco.
    public DbSet<Jogo> Jogos => Set<Jogo>();
    public DbSet<Promocao> Promocoes => Set<Promocao>();
    public DbSet<Plataforma> Plataformas => Set<Plataforma>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Endereco> Enderecos => Set<Endereco>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<Aluguel> Alugueis => Set<Aluguel>();
    public DbSet<Troca> Trocas => Set<Troca>();

    // Aqui refinamos o mapeamento (tamanhos, precisão, relacionamentos e dados iniciais).
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Tamanhos de texto (sem isto, o EF cria nvarchar(MAX), que é ruim) ----
        modelBuilder.Entity<Plataforma>().Property(p => p.Nome).HasMaxLength(50).IsRequired();
        modelBuilder.Entity<Genero>().Property(g => g.Nome).HasMaxLength(50).IsRequired();
        modelBuilder.Entity<Jogo>().Property(j => j.Titulo).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Jogo>().Property(j => j.Descricao).HasMaxLength(1000);
        modelBuilder.Entity<Cliente>().Property(c => c.Nome).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<Cliente>().Property(c => c.Telefone).HasMaxLength(20);
        modelBuilder.Entity<Cliente>().Property(c => c.CpfCnpj).HasMaxLength(14);   // só dígitos (CPF 11 / CNPJ 14)

        // ---- Endereço (agenda do cliente): Cliente (1) → Endereco (N). FK na Endereco. ----
        // Restrict = não deixa apagar um cliente que tenha endereços (evita cascata perigosa).
        modelBuilder.Entity<Endereco>(e =>
        {
            e.Property(x => x.Cep).HasMaxLength(8).IsRequired();          // só dígitos
            e.Property(x => x.Logradouro).HasMaxLength(150).IsRequired();
            e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            e.Property(x => x.Complemento).HasMaxLength(100);
            e.Property(x => x.Bairro).HasMaxLength(80).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(80).IsRequired();
            e.Property(x => x.Uf).HasMaxLength(2).IsRequired();
            e.HasOne(x => x.Cliente)
             .WithMany(c => c.Enderecos)
             .HasForeignKey(x => x.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Promoção: Jogo (1) → Promocao (N). FK na Promocao (lado "muitos"). ----
        // Índice nomeado DE PROPÓSITO (nada de _dta_index_ do legado): a consulta típica é
        // "promoções vigentes deste jogo" → índice por (JogoId, Ativa).
        modelBuilder.Entity<Promocao>(p =>
        {
            p.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            p.Property(x => x.PrecoPromocional).HasPrecision(10, 2);
            p.HasOne(x => x.Jogo)
             .WithMany(j => j.Promocoes)
             .HasForeignKey(x => x.JogoId)
             .OnDelete(DeleteBehavior.Cascade);   // apagou o jogo → promoções dele vão junto
            p.HasIndex(x => new { x.JogoId, x.Ativa }).HasDatabaseName("IX_Promocao_Jogo_Ativa");
        });

        // ---- Endereço de ENTREGA do pedido: owned type (snapshot embutido no Pedido). ----
        // Vira colunas EnderecoEntrega_Cep, _Logradouro... na própria tabela Pedido.
        // Não há tabela separada nem FK solta → IMPOSSÍVEL um pedido ter 2 endereços (§5.1).
        modelBuilder.Entity<Pedido>().OwnsOne(p => p.EnderecoEntrega, ee =>
        {
            ee.Property(x => x.Cep).HasMaxLength(8);
            ee.Property(x => x.Logradouro).HasMaxLength(150);
            ee.Property(x => x.Numero).HasMaxLength(20);
            ee.Property(x => x.Complemento).HasMaxLength(100);
            ee.Property(x => x.Bairro).HasMaxLength(80);
            ee.Property(x => x.Cidade).HasMaxLength(80);
            ee.Property(x => x.Uf).HasMaxLength(2);
        });

        // ---- Precisão dos valores em dinheiro: decimal(10,2) = até 99.999.999,99 ----
        modelBuilder.Entity<Jogo>().Property(j => j.PrecoVenda).HasPrecision(10, 2);
        modelBuilder.Entity<Jogo>().Property(j => j.PrecoAluguelDia).HasPrecision(10, 2);
        modelBuilder.Entity<Pedido>().Property(p => p.ValorTotal).HasPrecision(10, 2);
        modelBuilder.Entity<ItemPedido>().Property(i => i.PrecoUnitario).HasPrecision(10, 2);
        modelBuilder.Entity<Aluguel>().Property(a => a.ValorTotal).HasPrecision(10, 2);

        // ---- Troca aponta para 1 cliente e 2 jogos: precisamos configurar à mão ----
        // DeleteBehavior.Restrict = NÃO deixa apagar um jogo/cliente que esteja numa troca
        // (evita exclusões em cascata perigosas e o erro de "múltiplos caminhos de cascata" do SQL Server).
        modelBuilder.Entity<Troca>()
            .HasOne(t => t.ClienteOfertante)
            .WithMany(c => c.Trocas)
            .HasForeignKey(t => t.ClienteOfertanteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receptor: FK opcional (int?) — sem navegação de volta no Cliente (WithMany() vazio).
        modelBuilder.Entity<Troca>()
            .HasOne(t => t.ClienteReceptor)
            .WithMany()
            .HasForeignKey(t => t.ClienteReceptorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Troca>()
            .HasOne(t => t.JogoOferecido)
            .WithMany()
            .HasForeignKey(t => t.JogoOferecidoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Troca>()
            .HasOne(t => t.JogoDesejado)
            .WithMany()
            .HasForeignKey(t => t.JogoDesejadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Aluguel e ItemPedido também apontam para Jogo: mesma proteção (não apagar jogo em uso).
        modelBuilder.Entity<Aluguel>()
            .HasOne(a => a.Jogo)
            .WithMany()
            .HasForeignKey(a => a.JogoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemPedido>()
            .HasOne(i => i.Jogo)
            .WithMany()
            .HasForeignKey(i => i.JogoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---- Dados iniciais (seed): já nascem no banco quando aplicamos a migração ----
        modelBuilder.Entity<Plataforma>().HasData(
            new Plataforma { Id = 1, Nome = "PC" },
            new Plataforma { Id = 2, Nome = "PlayStation" },
            new Plataforma { Id = 3, Nome = "Xbox" },
            new Plataforma { Id = 4, Nome = "Nintendo Switch" }
        );

        modelBuilder.Entity<Genero>().HasData(
            new Genero { Id = 1, Nome = "Ação" },
            new Genero { Id = 2, Nome = "RPG" },
            new Genero { Id = 3, Nome = "Esporte" },
            new Genero { Id = 4, Nome = "Aventura" }
        );

        // Data fixa no seed (o EF exige valor constante aqui, não pode ser DateTime.Now).
        var dataSeed = new DateTime(2026, 6, 24);
        modelBuilder.Entity<Jogo>().HasData(
            new Jogo { Id = 1, Titulo = "God of War", PlataformaId = 2, GeneroId = 1, Condicao = CondicaoJogo.Usado, PrecoVenda = 150m, PrecoAluguelDia = 15m, QuantidadeEstoque = 3, Disponivel = true, DataCadastro = dataSeed, CriadoEm = dataSeed, CriadoPor = "seed" },
            new Jogo { Id = 2, Titulo = "The Witcher 3", PlataformaId = 1, GeneroId = 2, Condicao = CondicaoJogo.Usado, PrecoVenda = 90m, PrecoAluguelDia = 10m, QuantidadeEstoque = 5, Disponivel = true, DataCadastro = dataSeed, CriadoEm = dataSeed, CriadoPor = "seed" },
            new Jogo { Id = 3, Titulo = "EA Sports FC 24", PlataformaId = 3, GeneroId = 3, Condicao = CondicaoJogo.Novo, PrecoVenda = 250m, PrecoAluguelDia = 20m, QuantidadeEstoque = 2, Disponivel = true, DataCadastro = dataSeed, CriadoEm = dataSeed, CriadoPor = "seed" }
        );
    }
}
