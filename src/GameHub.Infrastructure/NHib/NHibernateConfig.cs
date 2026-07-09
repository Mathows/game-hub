using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;

namespace GameHub.Infrastructure.NHib;

/// <summary>
/// Monta a <b>ISessionFactory</b> do NHibernate. Comparando com o EF Core:
/// - <b>ISessionFactory</b> ~ é caro de criar → deve ser <b>Singleton</b> (1 por app).
/// - <b>ISession</b> (aberta pela factory) ~ é como o DbContext → <b>Scoped</b> (1 por usuário/uso).
/// </summary>
public static class NHibernateConfig
{
    public static ISessionFactory CriarSessionFactory(string connectionString)
    {
        var cfg = new Configuration();

        // Conexão + "dialeto" (qual SQL gerar) + driver (usa o Microsoft.Data.SqlClient, o mesmo do EF).
        cfg.DataBaseIntegration(db =>
        {
            db.ConnectionString = connectionString;
            db.Dialect<MsSql2012Dialect>();
            db.Driver<MicrosoftDataSqlClientDriver>();
        });

        // Registra os mapeamentos "por código".
        var mapper = new ModelMapper();
        mapper.AddMapping<ClienteMap>();
        mapper.AddMapping<JogoMap>();
        mapper.AddMapping<TrocaMap>();
        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

        return cfg.BuildSessionFactory();
    }
}
