using FastEndpoints;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.ListarClientes;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Lista os clientes";
        Description = "Retorna todos os clientes cadastrados, ordenados pelo Id.";
        Response<IReadOnlyList<ClienteResponse>>(200, "Coleção de clientes (vazia se não houver nenhum)");
    }
}
