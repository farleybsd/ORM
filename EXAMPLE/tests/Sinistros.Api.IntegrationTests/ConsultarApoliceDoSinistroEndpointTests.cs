using System.Net;
using System.Net.Http.Json;
using Sinistros.Api.Contracts;
using Sinistros.Api.Infrastructure.ApolicesApi;
using RegistrarSinistroRequest = Sinistros.Api.Features.RegistrarSinistro.Request;

namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Cobre GET /sinistros/{id}/apolice, que lê o sinistro localmente e consulta a apólice
/// vinculada na API de Apólices (via HTTP) a cada requisição.
/// </summary>
public class ConsultarApoliceDoSinistroEndpointTests
{
    private static readonly Guid ApoliceId = Guid.NewGuid();

    [Fact]
    public async Task DeveRetornar200ComOsDadosDaApolice_QuandoSinistroExiste()
    {
        await using var factory = new SinistrosApiFactory((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())));

        var client = factory.CreateClient();
        var sinistroId = await RegistrarSinistroAsync(client);

        var response = await client.GetAsync($"/sinistros/{sinistroId}/apolice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApoliceDoSinistroResponse>();
        Assert.NotNull(body);
        Assert.Equal(sinistroId, body!.SinistroId);
        Assert.Equal(ApoliceId, body.ApoliceId);
        Assert.Equal(new DateOnly(2026, 1, 1), body.VigenciaInicio);
        Assert.Equal(new DateOnly(2026, 12, 31), body.VigenciaFim);
        Assert.Equal(["Colisao"], body.Coberturas);
        Assert.True(body.VigenteNaDataOcorrencia);
        Assert.True(body.PossuiCoberturaDoSinistro);
    }

    [Fact]
    public async Task DeveRetornar404_QuandoSinistroNaoExiste()
    {
        await using var factory = new SinistrosApiFactory((_, _) =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())));

        var response = await factory.CreateClient().GetAsync($"/sinistros/{Guid.NewGuid()}/apolice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await ProblemaAssert.EhProblemaAsync(response, "sinistro-nao-encontrado");
        Assert.Equal(0, factory.Handler.NumeroDeChamadas); // nem chegou a chamar a API de Apólices
    }

    [Fact]
    public async Task DeveRetornar404_QuandoApoliceNaoExisteMaisNaApiDeApolices()
    {
        var apoliceExiste = true;

        await using var factory = new SinistrosApiFactory((_, _) => Task.FromResult(
            apoliceExiste
                ? JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())
                : new HttpResponseMessage(HttpStatusCode.NotFound)));

        var client = factory.CreateClient();
        var sinistroId = await RegistrarSinistroAsync(client);
        apoliceExiste = false;

        var response = await client.GetAsync($"/sinistros/{sinistroId}/apolice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problema = await ProblemaAssert.EhProblemaAsync(response, "apolice-nao-encontrada");
        Assert.Equal(ApoliceId, problema.GetProperty("apoliceId").GetGuid());
    }

    [Fact]
    public async Task DeveRetornar503_QuandoApiDeApolicesEstaIndisponivel()
    {
        var disponivel = true;

        await using var factory = new SinistrosApiFactory(
            (_, _) => Task.FromResult(
                disponivel
                    ? JsonResponse(HttpStatusCode.OK, ApoliceVigenteComColisao())
                    : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            opcoes =>
            {
                opcoes.Retry.MaxRetryAttempts = 1;
                opcoes.Retry.BaseDelay = TimeSpan.FromMilliseconds(10);
                opcoes.CircuitBreaker.MinimumThroughput = 1000; // não deixa o circuito interferir neste teste
            });

        var client = factory.CreateClient();
        var sinistroId = await RegistrarSinistroAsync(client);
        disponivel = false;

        var response = await client.GetAsync($"/sinistros/{sinistroId}/apolice");

        Assert.Equal((HttpStatusCode)503, response.StatusCode);
        await ProblemaAssert.EhProblemaAsync(response, "apolices-api-indisponivel");
    }

    private static async Task<Guid> RegistrarSinistroAsync(HttpClient client)
    {
        var registro = await client.PostAsJsonAsync("/sinistros", RequestValido());
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        var sinistro = await registro.Content.ReadFromJsonAsync<SinistroResponse>();
        return sinistro!.Id;
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
