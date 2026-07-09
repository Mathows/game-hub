using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using GameHub.Domain.Entities;

namespace GameHub.Infrastructure.NHib;

// No NHibernate, o mapeamento é EXPLÍCITO (aqui, "por código"). No EF Core, o DbContext
// descobre muita coisa por convenção. Esta é uma das diferenças que a Fase 5 quer mostrar.
//
// Mapeamos só o necessário para as Trocas: Troca + Cliente + Jogo (parcial).
// Lazy(false) = carrega direto (sem proxy), então as entidades não precisam ser "virtual".

public class ClienteMap : ClassMapping<Cliente>
{
    public ClienteMap()
    {
        Table("Clientes");
        Lazy(false);
        Id(x => x.Id, m => m.Generator(Generators.Identity));
        Property(x => x.Nome);
        Property(x => x.Telefone);
        Property(x => x.Endereco);
        Property(x => x.ApplicationUserId);
        Property(x => x.DataCadastro);
    }
}

public class JogoMap : ClassMapping<Jogo>
{
    public JogoMap()
    {
        Table("Jogos");
        Lazy(false);
        Id(x => x.Id, m => m.Generator(Generators.Identity));
        Property(x => x.Titulo);   // só o que usamos nas telas de troca
    }
}

public class TrocaMap : ClassMapping<Troca>
{
    public TrocaMap()
    {
        Table("Trocas");
        Lazy(false);
        Id(x => x.Id, m => m.Generator(Generators.Identity));
        Property(x => x.Status);         // enum → gravado como int (igual ao EF)
        Property(x => x.DataProposta);

        // Relacionamentos: o many-to-one "é dono" da coluna FK (por isso NÃO mapeamos
        // as propriedades escalares XxxId — senão a coluna ficaria mapeada duas vezes).
        ManyToOne(x => x.ClienteOfertante, m => { m.Column("ClienteOfertanteId"); m.NotNullable(true); });
        ManyToOne(x => x.ClienteReceptor, m => m.Column("ClienteReceptorId"));
        ManyToOne(x => x.JogoOferecido, m => m.Column("JogoOferecidoId"));
        ManyToOne(x => x.JogoDesejado, m => m.Column("JogoDesejadoId"));
    }
}
