using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Common;
using My_Orm_postgres.Api.Domain;

namespace My_Orm_postgres.Api.Features.ExcluirCliente;

public sealed class Handler(IOrm db)
{
    public async Task<Result> HandleAsync(long id, CancellationToken cancellationToken)
    {
        var cliente = new Cliente();
        if (!await cliente.LoadAsync(db, id, cancellationToken))
            return Result.Failure(ErrosDeCliente.ClienteNaoEncontrado(id));

        await cliente.DeleteAsync(db, cancellationToken);
        return Result.Success();
    }
}
