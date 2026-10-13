using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Infrastructure.ApolicesApi;

public static class ApolicesApiClientServiceCollectionExtensions
{
    /// <summary>
    /// Registra o cliente HTTP tipado usado para consultar a API de Apólices, já com o pipeline
    /// de resiliência (retry, circuit breaker e timeout) configurado a partir da seção
    /// <see cref="ApolicesApiOptions.SectionName"/> de <paramref name="configuration"/>.
    /// </summary>
    /// <remarks>
    /// O endereço da API de Apólices nunca é fixo no código: ele é resolvido em tempo de
    /// execução pela cadeia de configuração do ASP.NET Core, na ordem
    /// <c>appsettings.json</c> → <c>appsettings.{ASPNETCORE_ENVIRONMENT}.json</c> →
    /// variáveis de ambiente (<c>ApolicesApi__BaseUrl</c>) → argumentos de linha de comando.
    /// Assim a mesma imagem/binário aponta para localhost na máquina do desenvolvedor, para o
    /// endereço de QAS quando <c>ASPNETCORE_ENVIRONMENT=QAS</c> e para o de produção quando
    /// <c>ASPNETCORE_ENVIRONMENT=Production</c>.
    /// </remarks>
    public static IServiceCollection AddApolicesApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<ApolicesApiOptions>()
            .Bind(configuration.GetSection(ApolicesApiOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                $"'{ApolicesApiOptions.SectionName}:BaseUrl' precisa ser uma URL http(s) absoluta. " +
                "Defina-a no appsettings.{ASPNETCORE_ENVIRONMENT}.json do ambiente ou na variável " +
                "de ambiente 'ApolicesApi__BaseUrl'.")
            // Falha já na subida da aplicação (e não na primeira requisição) se o ambiente
            // estiver sem o endereço da API de Apólices.
            .ValidateOnStart();

        services
            .AddHttpClient<IApolicesApiClient, ApolicesApiClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ApolicesApiOptions>>().Value;
                client.BaseAddress = options.BaseUri();
            })
            .AddResilienceHandler("apolices-pipeline", (pipelineBuilder, context) =>
            {
                var options = context.ServiceProvider.GetRequiredService<IOptions<ApolicesApiOptions>>().Value;

                // Ordem: Retry (mais externo) -> Circuit Breaker -> Timeout por tentativa (mais interno).
                // Só GET é feito aqui (consulta), então repetir a chamada é seguro.
                pipelineBuilder
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = options.Retry.MaxRetryAttempts,
                        Delay = options.Retry.BaseDelay
                    })
                    .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = options.CircuitBreaker.FailureRatio,
                        SamplingDuration = options.CircuitBreaker.SamplingDuration,
                        MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                        BreakDuration = options.CircuitBreaker.BreakDuration
                    })
                    .AddTimeout(options.Timeout);
            });

        return services;
    }
}
