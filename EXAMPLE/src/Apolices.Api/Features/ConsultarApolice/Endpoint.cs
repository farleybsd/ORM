using Apolices.Api.Contracts;
using Apolices.Api.Infrastructure.Erros;
using FastEndpoints;

namespace Apolices.Api.Features.ConsultarApolice;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ApoliceResponse>
{
    public override void Configure()
    {
        Get("/apolices/{id}");
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
