using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sinistros.Api.Domain;
using Sinistros.Api.Infrastructure.ApolicesApi;
namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Uma instância nova é criada por teste (não é um fixture compartilhado) para que o estado do
/// circuit breaker de um teste nunca vaze para o próximo.
/// </summary>
public sealed class SinistrosApiFactory(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
    Action<ApolicesApiOptions>? sobrescreverOpcoes = null,
    Action<IServiceCollection>? sobrescreverServicos = null,
    string ambiente = "Development") : WebApplicationFactory<Program>
{
    public ScriptedHttpMessageHandler Handler { get; } = new(responder);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Os testes gravam no banco real configurado no appsettings (suba antes: container/docker compose up -d)
        builder.UseEnvironment(ambiente);

        builder.ConfigureTestServices(services =>
        {
            services
                .AddHttpClient<IApolicesApiClient, ApolicesApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Handler);

            if (sobrescreverOpcoes is not null)
                services.PostConfigure(sobrescreverOpcoes);

            sobrescreverServicos?.Invoke(services);
        });
    }
}
