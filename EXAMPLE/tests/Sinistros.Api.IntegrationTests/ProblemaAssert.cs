using System.Text.Json;

namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Verifica que uma resposta de erro segue o contrato RFC 9457 da API e devolve o corpo para
/// asserções adicionais sobre as extensões.
/// </summary>
public static class ProblemaAssert
{
    public static async Task<JsonElement> EhProblemaAsync(HttpResponseMessage response, string codigoEsperado)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problema = documento.RootElement.Clone();

        Assert.Equal((int)response.StatusCode, problema.GetProperty("status").GetInt32());
        Assert.Equal($"https://seguros.example/problemas/{codigoEsperado}", problema.GetProperty("type").GetString());
        Assert.Equal(codigoEsperado, problema.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("instance").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("traceId").GetString()));

        return problema;
    }
}
