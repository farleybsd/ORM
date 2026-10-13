using Apolices.Api.Contracts;
using FastEndpoints;

namespace Apolices.Api.Features.ListarApolices;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<IReadOnlyList<ApoliceResponse>>
{
    public override void Configure()
    {
        Get("/apolices");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(ct);
        return await Send.OkAsync(resultado, ct);
    }
}
