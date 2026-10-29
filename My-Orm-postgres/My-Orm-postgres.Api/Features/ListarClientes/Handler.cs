using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.ListarClientes;

public sealed class Handler(IOrm db, ILogger<Handler> logger)
{
    public async Task<IReadOnlyList<ClienteResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var clientes = await db.QueryAsync<Cliente>("SELECT * FROM cliente ORDER BY id", ct: cancellationToken);
        logger.LogInformation("Listagem retornou {Quantidade} clientes", clientes.Count);
        return clientes.Select(CriarCliente.Handler.Mapear).ToList();
    }
}
