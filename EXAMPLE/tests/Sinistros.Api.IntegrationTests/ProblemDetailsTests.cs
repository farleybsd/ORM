using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Sinistros.Api.Domain;
using RegistrarSinistroRequest = Sinistros.Api.Features.RegistrarSinistro.Request;

namespace Sinistros.Api.IntegrationTests;

/// <summary>
/// Garante o contrato de erros RFC 9457 para o que não vem de um caso de uso: erros inesperados
/// (exceções), erros de validação e rotas inexistentes. Rodam em <c>Production</c>, o ambiente
/// em que um vazamento de detalhes internos realmente importa.
/// </summary>
public class ProblemDetailsTests
{
    [Fact]
    public async Task ErroInesperado_NaoVazaDetalhesInternos()
    {
        await using var factory = new SinistrosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
            sobrescreverServicos: servicos => servicos.AddSingleton<ISinistroRepository, RepositorioQueFalha>(),
            ambiente: "Production");

        var resposta = await factory.CreateClient().GetAsync($"/sinistros/{Guid.NewGuid()}");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        await ProblemaAssert.EhProblemaAsync(resposta, "erro-interno");
        Assert.DoesNotContain("segredo", corpo);
        Assert.DoesNotContain(nameof(InvalidOperationException), corpo);
        Assert.DoesNotContain(nameof(RepositorioQueFalha), corpo);
    }

    [Fact]
    public async Task ErroDeValidacao_RetornaValidationProblemDetails()
    {
        await using var factory = new SinistrosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
            ambiente: "Production");

        var resposta = await factory.CreateClient().PostAsJsonAsync("/sinistros", new RegistrarSinistroRequest
        {
            ApoliceId = Guid.Empty,
            DataOcorrencia = new DateOnly(2026, 6, 1),
            TipoCobertura = "Inexistente",
            Descricao = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await ProblemaAssert.EhProblemaAsync(resposta, "validacao");
        var erros = problema.GetProperty("errors");
        Assert.True(erros.TryGetProperty("apoliceId", out _) || erros.TryGetProperty("ApoliceId", out _));
        Assert.Equal(0, factory.Handler.NumeroDeChamadas);
    }

    [Fact]
    public async Task RotaInexistente_RetornaProblemDetails()
    {
        await using var factory = new SinistrosApiFactory(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
            ambiente: "Production");

        var resposta = await factory.CreateClient().GetAsync("/rota-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    private sealed class RepositorioQueFalha : ISinistroRepository
    {
        private static Exception Falha() => new InvalidOperationException("Host=db-prod;Password=segredo123");

        public Task AdicionarAsync(Sinistro sinistro, CancellationToken cancellationToken) => throw Falha();

        public Task<Sinistro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) => throw Falha();

        public Task<IReadOnlyList<Sinistro>> ListarPorApoliceAsync(Guid apoliceId, CancellationToken cancellationToken) => throw Falha();

        public Task<Sinistros.Api.Common.Result> AtualizarAsync(Sinistro sinistro, int versaoEsperada, CancellationToken cancellationToken) => throw Falha();

        public Task<Sinistros.Api.Common.Result> RemoverAsync(Guid id, int versaoEsperada, CancellationToken cancellationToken) => throw Falha();
    }
}
