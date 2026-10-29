using FastEndpoints;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Infrastructure.Erros;

namespace My_Orm_postgres.Api.Features.ConsultarCliente;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<ClienteResponse>
{
    public override void Configure()
    {
        Get("/clientes/{id}");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var id = Route<long>("id");
        var resultado = await handler.HandleAsync(id, ct);

        return resultado.IsFailure
            ? await Send.ResultAsync(resultado.Error!.ParaProblema())
            : await Send.OkAsync(resultado.Value, ct);
    }
}
