using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Common;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Domain;

namespace My_Orm_postgres.Api.Features.ConsultarCliente;

public sealed class Handler(IOrm db, ILogger<Handler> logger)
{
    public async Task<Result<ClienteResponse>> HandleAsync(long id, CancellationToken cancellationToken)
    {
        var cliente = new Cliente();
        if (!await cliente.LoadAsync(db, id, cancellationToken))
        {
            logger.LogInformation("Cliente {ClienteId} não encontrado", id);
            return ErrosDeCliente.ClienteNaoEncontrado(id);
        }

        return Result.Success(CriarCliente.Handler.Mapear(cliente));
    }
}
