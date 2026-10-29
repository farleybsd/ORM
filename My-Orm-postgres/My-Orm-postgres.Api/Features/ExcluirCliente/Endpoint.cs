using FastEndpoints;
using My_Orm_postgres.Api.Infrastructure.Erros;

namespace My_Orm_postgres.Api.Features.ExcluirCliente;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/clientes/{id}");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<long>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.NoContentAsync(ct);
    }
}
