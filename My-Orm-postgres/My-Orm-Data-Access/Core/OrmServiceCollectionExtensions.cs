using Microsoft.Extensions.DependencyInjection;

namespace My_Orm_Data_Access.Core;

public static class OrmServiceCollectionExtensions
{
    // builder.Services.AddOrm(o => o.UsePostgres(connectionString));
    public static IServiceCollection AddOrm(this IServiceCollection services, Action<OrmOptions> configure)
    {
        var options = new OrmOptions();
        configure(options);

        var factory = options.SessionFactory
            ?? throw new InvalidOperationException(
                "Nenhum driver configurado. Chame o.UsePostgres(...) dentro do AddOrm.");

        // Scoped = uma sessão por requisição: mesma conexão/transação para todos os models
        // do request, e o Dispose no fim do request faz rollback do que não teve Commit
        services.AddScoped(factory);
        return services;
    }
}
