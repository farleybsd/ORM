using System.Net;
using System.Net.Http.Json;
using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;
using Sinistros.Api.Infrastructure.ApolicesApi;
using RegistrarSinistroRequest = Sinistros.Api.Features.RegistrarSinistro.Request;

namespace Sinistros.Api.IntegrationTests;

public class RegistrarSinistroEndpointTests
{
    private static readonly Guid ApoliceId = Guid.NewGuid();

    [Fact]
    public async Task DeveRetornar201_QuandoApoliceVigenteEComCobertura()
    {
        await using var factory = new SinistrosApiFactory((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())));

        var response = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SinistroResponse>();
        Assert.NotNull(body);
        Assert.Equal(ApoliceId, body!.ApoliceId);
    }

    [Fact]
    public async Task DeveRetornar404_QuandoApoliceNaoExiste()
    {
        await using var factory = new SinistrosApiFactory((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var response = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ProblemaAssert.EhProblemaAsync(response, "apolice-nao-encontrada");
    }

    [Fact]
    public async Task DeveRetornar422_QuandoApoliceNaoPossuiACoberturaSolicitada()
    {
        await using var factory = new SinistrosApiFactory((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())));

        var request = RequestValido();
        request.TipoCobertura = "Roubo";

        var response = await factory.CreateClient().PostAsJsonAsync("/sinistros", request);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        var problema = await ProblemaAssert.EhProblemaAsync(response, "cobertura-nao-contratada");
        Assert.Equal("Roubo", problema.GetProperty("tipoCobertura").GetString());
    }

    [Fact]
    public async Task DeveRecuperarViaRetry_QuandoDuasFalhasTransitoriasSeguidasDeSucesso()
    {
        var tentativa = 0;

        await using var factory = new SinistrosApiFactory(
            (_, _) =>
            {
                tentativa++;
                var resposta = tentativa <= 2
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao());
                return Task.FromResult(resposta);
            },
            opcoes =>
            {
                opcoes.Retry.MaxRetryAttempts = 3;
                opcoes.Retry.BaseDelay = TimeSpan.FromMilliseconds(10);
                opcoes.CircuitBreaker.MinimumThroughput = 1000; // não deixa o circuito interferir neste teste
            });

        var response = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(3, factory.Handler.NumeroDeChamadas);
    }

    [Fact]
    public async Task DeveAbrirCircuito_QuandoFalhasPersistentesUltrapassamOLimiar()
    {
        // MaxRetryAttempts mínimo permitido pelo Polly é 1 (não é possível desativar o retry).
        // Com 1 retry, a primeira chamada já esgota 2 tentativas, o que basta para atingir o
        // MinimumThroughput do circuito e abri-lo antes da segunda chamada.
        await using var factory = new SinistrosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            opcoes =>
            {
                opcoes.Retry.MaxRetryAttempts = 1;
                opcoes.Retry.BaseDelay = TimeSpan.FromMilliseconds(10);
                opcoes.CircuitBreaker.MinimumThroughput = 2;
                opcoes.CircuitBreaker.FailureRatio = 0.5;
                opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
                opcoes.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
            });

        var client = factory.CreateClient();

        var primeiraResposta = await client.PostAsJsonAsync("/sinistros", RequestValido());
        Assert.Equal((HttpStatusCode)503, primeiraResposta.StatusCode);
        Assert.Equal(2, factory.Handler.NumeroDeChamadas); // 1 tentativa + 1 retry, ambas falhando

        var segundaResposta = await client.PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal((HttpStatusCode)503, segundaResposta.StatusCode);
        Assert.Equal(2, factory.Handler.NumeroDeChamadas); // circuito já aberto: handler não foi chamado de novo
    }

    [Fact]
    public async Task DeveRetornar503_QuandoConsultaExcedeOTimeoutConfigurado()
    {
        // MaxRetryAttempts=1 (mínimo permitido): a chamada e a única retentativa esgotam o
        // tempo limite por tentativa, resultando em indisponibilidade.
        await using var factory = new SinistrosApiFactory(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                return JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao());
            },
            opcoes =>
            {
                opcoes.Timeout = TimeSpan.FromMilliseconds(200);
                opcoes.Retry.MaxRetryAttempts = 1;
                opcoes.Retry.BaseDelay = TimeSpan.FromMilliseconds(10);
                opcoes.CircuitBreaker.MinimumThroughput = 1000;
            });

        var response = await factory.CreateClient().PostAsJsonAsync("/sinistros", RequestValido());

        Assert.Equal((HttpStatusCode)503, response.StatusCode);
        Assert.Equal(2, factory.Handler.NumeroDeChamadas);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object corpo) => new(statusCode)
    {
        Content = JsonContent.Create(corpo)
    };

    private static ApoliceApiDto ApoliceVigenteComColisao() => new(
        ApoliceId,
        "AP-0001",
        new SeguradoApiDto("Ana Souza", "111.111.111-11"),
        new VigenciaApiDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
        [new CoberturaApiDto("Colisao", 50_000m)]);

    private static RegistrarSinistroRequest RequestValido() => new()
    {
        ApoliceId = ApoliceId,
        DataOcorrencia = new DateOnly(2026, 6, 1),
        TipoCobertura = "Colisao",
        Descricao = "Colisão na Av. Paulista"
    };
}
