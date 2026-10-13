using FastEndpoints;
using Sinistros.Api.Infrastructure.Erros;

namespace Sinistros.Api.Features.ExcluirSinistro;

public sealed class Endpoint(Handler handler) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("/sinistros/{id}");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.NoContentAsync(ct);
    }
}
