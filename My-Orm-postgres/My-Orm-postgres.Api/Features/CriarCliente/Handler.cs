using My_Orm_Data_Access.Core;
using My_Orm_Data_Access.Models;
using My_Orm_postgres.Api.Common;
using My_Orm_postgres.Api.Contracts;
using My_Orm_postgres.Api.Domain;

namespace My_Orm_postgres.Api.Features.CriarCliente;

public sealed class Handler(IOrm db)
{
    public async Task<Result<ClienteResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var cliente = new Cliente
        {
            Nome = request.Nome,
            Email = request.Email,
            Ativo = request.Ativo
        };

        try
        {
            await cliente.SaveAsync(db, cancellationToken);
        }
        catch (OrmException ex) when (ex.Kind == OrmErrorKind.Duplicate)
        {
            return ErrosDeCliente.EmailJaCadastrado(request.Email);
        }

        return Result.Success(Mapear(cliente));
    }

    internal static ClienteResponse Mapear(Cliente cliente) => new(
        cliente.Id,
        cliente.Nome,
        cliente.Email,
        cliente.Ativo,
        DateTimeOffset.FromUnixTimeMilliseconds(cliente.InsertedAt),
        DateTimeOffset.FromUnixTimeMilliseconds(cliente.UpdatedAt),
        cliente.UpdatedAt);
}
