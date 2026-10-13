using Apolices.Api.Domain;
using DDDWithHttp.Orm.DependencyInjection;
using DDDWithHttp.Orm.Sessions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apolices.Api.Infrastructure.Persistencia;

public static class PersistenciaServiceCollectionExtensions
{
    /// <summary>
    /// Registra o ORM (com TODOS os bancos da seção "Orm", que podem ser usados ao mesmo tempo)
    /// e o repositório no banco escolhido em <c>Persistencia:Banco</c>.
    /// Para trocar de banco sem recompilar: variável de ambiente <c>Persistencia__Banco=SqlServer</c>.
    /// </summary>
    public static IServiceCollection AddPersistenciaDeApolices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOrm(configuration);
        services.TryAddSingleton(TimeProvider.System);

        var banco = (configuration.GetSection(PersistenciaOptions.SectionName).Get<PersistenciaOptions>()
                     ?? new PersistenciaOptions()).Banco;

        if (!configuration.GetConfiguredOrmProviders().Contains(banco))
            throw new InvalidOperationException(
                $"'{PersistenciaOptions.SectionName}:{nameof(PersistenciaOptions.Banco)}' aponta para '{banco}', " +
                "mas esse banco não está em 'Orm:Connections'.");

        // Scoped: o repositório usa a sessão da requisição (Unit of Work)
        services.AddScoped<IApoliceRepository>(sp => new OrmApoliceRepository(
            sp.GetRequiredService<IDbSessionProvider>(),
            banco,
            sp.GetRequiredService<TimeProvider>()));

        return services;
    }
}
