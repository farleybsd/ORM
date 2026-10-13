using FastEndpoints;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ListarSinistrosPorApolice;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<IReadOnlyList<SinistroResponse>>
{
    public override void Configure()
    {
        Get("/apolices/{apoliceId}/sinistros");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var apoliceId = Route<Guid>("apoliceId");
        var resultado = await handler.HandleAsync(apoliceId, ct);
        return await Send.OkAsync(resultado, ct);
    }
}
