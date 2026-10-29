using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace My_Orm_postgres.Api.Features.ExcluirCliente;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Exclui um cliente";
        Description = "Remove o cliente definitivamente.";
        Params["id"] = "Identificador do cliente";
        Response(204, "Cliente excluído");
        Response<ProblemDetails>(404, "Nenhum cliente foi encontrado com o Id informado (code: cliente-nao-encontrado)", "application/problem+json");
    }
}
