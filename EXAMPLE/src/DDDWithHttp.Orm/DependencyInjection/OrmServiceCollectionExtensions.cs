using DDDWithHttp.Orm.Configuration;
using DDDWithHttp.Orm.HealthChecks;
using DDDWithHttp.Orm.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DDDWithHttp.Orm.DependencyInjection;

public static class OrmServiceCollectionExtensions
{
    private const string HealthCheckName = "orm";

    /// <summary>
    /// Registra o ORM lendo a seção <see cref="OrmOptions.SectionName"/>:
    /// <list type="bullet">
    /// <item><see cref="IDbSessionFactory"/> (singleton): cria sessões avulsas.</item>
    /// <item><see cref="IDbSessionProvider"/> (scoped): uma sessão por banco por requisição (Unit of Work).</item>
    /// </list>
    /// A configuração é validada na subida da aplicação, não na primeira requisição.
    /// </summary>
    public static IServiceCollection AddOrm(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OrmOptions>()
            .Bind(configuration.GetSection(OrmOptions.SectionName))
            .Validate(o => o.Connections.Count > 0,
                $"Configure ao menos um banco em '{OrmOptions.SectionName}:Connections' " +
                $"({string.Join(", ", Enum.GetNames<DatabaseProvider>())}).")
            .Validate(o => o.Connections.Values.All(c => !string.IsNullOrWhiteSpace(c.ConnectionString)),
                $"Toda conexão em '{OrmOptions.SectionName}:Connections' precisa de ConnectionString.")
            .Validate(o => o.Connections.Values.All(c => c.CommandTimeoutSeconds > 0),
                "CommandTimeoutSeconds precisa ser maior que zero.")
            .ValidateOnStart();

        services.AddSingleton<IDbSessionFactory, DbSessionFactory>();
        services.AddScoped<IDbSessionProvider, DbSessionProvider>();

        return services;
    }

    /// <summary>Adiciona um health check que testa todos os bancos configurados.</summary>
    public static IHealthChecksBuilder AddOrmHealthCheck(this IHealthChecksBuilder builder)
        => builder.AddCheck<OrmHealthCheck>(HealthCheckName);

    /// <summary>Lê da configuração quais bancos estão configurados (útil para validar escolhas na subida).</summary>
    public static IReadOnlyCollection<DatabaseProvider> GetConfiguredOrmProviders(this IConfiguration configuration)
        => configuration.GetSection(OrmOptions.SectionName).Get<OrmOptions>()?.Connections.Keys
           ?? (IReadOnlyCollection<DatabaseProvider>)[];
}
