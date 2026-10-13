using DDDWithHttp.Orm.Configuration;
using DDDWithHttp.Orm.Dialects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DDDWithHttp.Orm.Sessions;

internal sealed class DbSessionFactory(IOptions<OrmOptions> options, ILoggerFactory loggerFactory) : IDbSessionFactory
{
    private static readonly IReadOnlyDictionary<DatabaseProvider, ISqlDialect> Dialects =
        new Dictionary<DatabaseProvider, ISqlDialect>
        {
            [DatabaseProvider.Postgres] = new PostgresDialect(),
            [DatabaseProvider.SqlServer] = new SqlServerDialect()
        };

    private readonly OrmOptions _options = options.Value;
    private readonly ILogger _logger = loggerFactory.CreateLogger<DbSession>();

    public IReadOnlyCollection<DatabaseProvider> ConfiguredProviders => _options.Connections.Keys;

    public bool IsConfigured(DatabaseProvider provider) => _options.Connections.ContainsKey(provider);

    public IDbSession Create(DatabaseProvider provider)
    {
        if (!_options.Connections.TryGetValue(provider, out OrmConnectionOptions? connection))
            throw new InvalidOperationException(
                $"O banco '{provider}' não está configurado em '{OrmOptions.SectionName}:Connections'.");

        return new DbSession(Dialects[provider], connection, _options.MaxTransactionAttempts, _logger);
    }
}
