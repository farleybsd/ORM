using FastEndpoints;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Infrastructure.Erros;

namespace My_Orm_postgres.Api.Features.CriarCliente;

public sealed class Endpoint(Handler handler) : Endpoint<Request, ClienteResponse>
{
    public override void Configure()
    {
        Post("/clientes");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(Request req, CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(req, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.ResponseAsync(resultado.Value, 201, ct);
    }
}
