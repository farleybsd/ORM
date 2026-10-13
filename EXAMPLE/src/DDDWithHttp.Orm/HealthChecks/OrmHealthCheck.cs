using DDDWithHttp.Orm.Sessions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DDDWithHttp.Orm.HealthChecks;

/// <summary>Verifica, com um SELECT 1, se cada banco configurado está respondendo.</summary>
internal sealed class OrmHealthCheck(IDbSessionFactory factory) : IHealthCheck
{
    private const string PingSql = "SELECT 1";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        bool healthy = true;

        foreach (DatabaseProvider provider in factory.ConfiguredProviders)
        {
            await using IDbSession session = factory.Create(provider);
            try
            {
                await session.ExecuteScalarAsync<int>(PingSql, ct: cancellationToken);
                data[provider.ToString()] = HealthStatus.Healthy.ToString();
            }
            catch (OrmException ex)
            {
                healthy = false;
                data[provider.ToString()] = ex.Kind.ToString();
            }
        }

        return healthy
            ? HealthCheckResult.Healthy("Todos os bancos responderam.", data)
            : HealthCheckResult.Unhealthy("Um ou mais bancos não responderam.", data: data);
    }
}
