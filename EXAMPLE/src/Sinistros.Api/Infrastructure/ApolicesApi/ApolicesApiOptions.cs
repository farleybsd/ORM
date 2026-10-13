namespace Sinistros.Api.Infrastructure.ApolicesApi;

public sealed class ApolicesApiOptions
{
    public const string SectionName = "ApolicesApi";

    /// <summary>
    /// Endereço base da API de Apólices. Não tem valor fixo no código: vem do
    /// <c>appsettings.{ASPNETCORE_ENVIRONMENT}.json</c> (localhost em Development, o endereço de
    /// QAS em QAS, o de produção em Production) e pode ser sobrescrito sem novo build pela
    /// variável de ambiente <c>ApolicesApi__BaseUrl</c>.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
    public RetryOptions Retry { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Converte <see cref="BaseUrl"/> no <see cref="Uri"/> usado como endereço base do
    /// HttpClient, garantindo a barra final — sem ela, um endereço com caminho
    /// (ex.: <c>https://gateway/apolices-api</c>, comum em QAS/PRD atrás de gateway) perderia
    /// esse caminho ao combinar com a rota relativa.
    /// </summary>
    public Uri BaseUri() => new(BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/");

    public sealed class RetryOptions
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    public sealed class CircuitBreakerOptions
    {
        public double FailureRatio { get; set; } = 0.5;
        public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);
        public int MinimumThroughput { get; set; } = 4;
        public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(15);
    }
}
