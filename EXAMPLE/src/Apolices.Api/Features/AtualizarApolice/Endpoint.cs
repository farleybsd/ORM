using Apolices.Api.Contracts;
using Apolices.Api.Infrastructure.Erros;
using FastEndpoints;

namespace Apolices.Api.Features.AtualizarApolice;

public sealed class Endpoint(Handler handler) : Endpoint<Request, ApoliceResponse>
{
    public override void Configure()
    {
        Put("/apolices/{id}");
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
