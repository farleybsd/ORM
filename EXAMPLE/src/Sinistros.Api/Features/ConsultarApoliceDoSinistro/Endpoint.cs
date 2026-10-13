using FastEndpoints;
using Sinistros.Api.Contracts;
using Sinistros.Api.Infrastructure.Erros;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ApoliceDoSinistroResponse>
{
    public override void Configure()
    {
        Get("/sinistros/{id}/apolice");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.OkAsync(resultado.Value, ct);
    }
}
