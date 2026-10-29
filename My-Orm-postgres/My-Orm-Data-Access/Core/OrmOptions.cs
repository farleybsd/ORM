using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using My_Orm_Data_Access.Driver;

namespace My_Orm_Data_Access.Core;

public class OrmOptions
{
    // Receita para criar a sessão: cada UseXxx escolhe o driver
    internal Func<IServiceProvider, IOrm>? SessionFactory { get; private set; }

    public OrmOptions UsePostgres(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "A connection string do Postgres está vazia. Confira o appsettings.json", nameof(connectionString));

        SessionFactory = sp => new Postgres(connectionString, sp.GetService<ILogger<Postgres>>());
        return this;
    }
}
