using System.Net;
using System.Net.Http.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Infrastructure.ApolicesApi;

/// <summary>
/// Adaptador HTTP da porta <see cref="IApolicesApiClient"/>. Chama a API de Apólices através do
/// HttpClient tipado configurado em Program.cs (timeout, retry e circuit breaker aplicados pelo
/// pipeline de resiliência do Microsoft.Extensions.Http.Resilience).
/// </summary>
public sealed class ApolicesApiClient(HttpClient httpClient, ILogger<ApolicesApiClient> logger) : IApolicesApiClient
{
    public async Task<ApoliceConsultaResultado> ConsultarApoliceAsync(Guid apoliceId, CancellationToken cancellationToken)
    {
        try
        {
            // Rota relativa (sem barra inicial): o endereço base vem do ambiente atual
            // (ApolicesApiOptions.BaseUrl) e pode conter um caminho, que seria descartado por uma
            // barra inicial aqui.
            using var response = await httpClient.GetAsync($"apolices/{apoliceId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new ApoliceConsultaResultado.NaoEncontrada();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Consulta à API de Apólices retornou {StatusCode} para a apólice {ApoliceId}.",
                    (int)response.StatusCode,
                    apoliceId);

                return new ApoliceConsultaResultado.Indisponivel($"Resposta inesperada da API de Apólices: {(int)response.StatusCode}.");
            }

            var dto = await response.Content.ReadFromJsonAsync<ApoliceApiDto>(cancellationToken);
            if (dto is null)
                return new ApoliceConsultaResultado.Indisponivel("Resposta vazia da API de Apólices.");

            return new ApoliceConsultaResultado.Encontrada(Traduzir(dto));
        }
        catch (TimeoutRejectedException)
        {
            logger.LogWarning("Tempo limite excedido ao consultar a apólice {ApoliceId} na API de Apólices.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("Tempo limite excedido ao consultar a API de Apólices.");
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("Circuito aberto para a API de Apólices — requisição para a apólice {ApoliceId} não foi enviada.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("API de Apólices temporariamente indisponível (circuito aberto).");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Falha de comunicação com a API de Apólices ao consultar a apólice {ApoliceId}.", apoliceId);
            return new ApoliceConsultaResultado.Indisponivel("Falha de comunicação com a API de Apólices.");
        }
    }

    private static ApoliceReferenciada Traduzir(ApoliceApiDto dto) => new(
        dto.Id,
        dto.Vigencia.Inicio,
        dto.Vigencia.Fim,
        dto.Coberturas.Select(c => Enum.Parse<TipoCobertura>(c.Tipo, ignoreCase: true)).ToList());
}
