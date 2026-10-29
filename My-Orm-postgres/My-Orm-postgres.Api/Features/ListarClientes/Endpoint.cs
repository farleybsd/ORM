using FastEndpoints;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.ListarClientes;

public sealed class Endpoint(Handler handler) : EndpointWithoutRequest<IReadOnlyList<ClienteResponse>>
{
    public override void Configure()
    {
        Get("/clientes");
        AllowAnonymous();
    }

    public override async Task<FastEndpoints.Void> HandleAsync(CancellationToken ct)
    {
        var resultado = await handler.HandleAsync(ct);
        return await Send.OkAsync(resultado, ct);
    }
}
