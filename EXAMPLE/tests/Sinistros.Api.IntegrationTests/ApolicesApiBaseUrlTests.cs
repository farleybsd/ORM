using System.Net;
using System.Net.Http.Json;
using Sinistros.Api.Infrastructure.ApolicesApi;
using RegistrarSinistroRequest = Sinistros.Api.Features.RegistrarSinistro.Request;

namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Garante que o endereço da API de Apólices vem da configuração do ambiente
/// (ApolicesApi:BaseUrl) e não de um valor fixo no código, inclusive quando o endereço do
/// ambiente inclui um caminho (cenário típico de QAS/PRD atrás de um gateway).
/// </summary>
public class ApolicesApiBaseUrlTests
{
    [Theory]
    [InlineData("http://localhost:5201", "http://localhost:5201/apolices/")]
    [InlineData("https://apolices.qas.seguros.example/", "https://apolices.qas.seguros.example/apolices/")]
    [InlineData("https://gateway.qas.seguros.example/apolices-api", "https://gateway.qas.seguros.example/apolices-api/apolices/")]
    public async Task DeveMontarAUrlDaConsultaAPartirDoBaseUrlDoAmbiente(string baseUrl, string prefixoEsperado)
    {
        Uri? urlChamada = null;

        await using var factory = new SinistrosApiFactory(
            (request, _) =>
            {
                urlChamada = request.RequestUri;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            },
            opcoes => opcoes.BaseUrl = baseUrl);

        var apoliceId = Guid.NewGuid();
        await factory.CreateClient().PostAsJsonAsync("/sinistros", new RegistrarSinistroRequest
        {
            ApoliceId = apoliceId,
            DataOcorrencia = new DateOnly(2026, 6, 1),
            TipoCobertura = "Colisao",
            Descricao = "Colisão na Av. Paulista"
        });

        Assert.Equal($"{prefixoEsperado}{apoliceId}", urlChamada?.ToString());
    }
}
