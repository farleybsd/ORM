using FastEndpoints;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Infrastructure.Erros;

namespace My_Orm_postgres.Api.Features.AtualizarCliente;

public sealed class Endpoint(Handler handler) : Endpoint<Request, ClienteResponse>
{
    public override void Configure()
    {
        Put("/clientes/{id}");
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
