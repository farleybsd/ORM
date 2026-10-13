using FastEndpoints;
using Sinistros.Api.Contracts;
using Sinistros.Api.Infrastructure.Erros;

namespace Sinistros.Api.Features.AtualizarSinistro;

public sealed class Endpoint(Handler handler) : Endpoint<Request, SinistroResponse>
{
    public override void Configure()
    {
        Put("/sinistros/{id}");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.OkAsync(resultado.Value, ct);
    }
}
