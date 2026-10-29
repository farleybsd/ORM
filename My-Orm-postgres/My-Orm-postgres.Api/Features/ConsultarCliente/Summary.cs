using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.ConsultarCliente;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Consulta um cliente pelo identificador";
        Description = "Retorna os dados de um cliente, incluindo a versão usada na atualização.";
        Params["id"] = "Identificador do cliente";
        Response<ClienteResponse>(200, "Cliente encontrado");
        Response<ProblemDetails>(404, "Nenhum cliente foi encontrado com o Id informado (code: cliente-nao-encontrado)", "application/problem+json");
    }
}
