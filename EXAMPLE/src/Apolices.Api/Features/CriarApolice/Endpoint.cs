using Apolices.Api.Contracts;
using Apolices.Api.Infrastructure.Erros;
using FastEndpoints;

namespace Apolices.Api.Features.CriarApolice;

public sealed class Endpoint(Handler handler) : Endpoint<Request, ApoliceResponse>
{
    public override void Configure()
    {
        Post("/apolices");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.ResponseAsync(resultado.Value, StatusCodes.Status201Created, ct);
    }
}
