using Apolices.Api.Contracts;
using FastEndpoints;

namespace Apolices.Api.Features.ListarApolices;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Lista todas as apólices cadastradas";
        Description = "Retorna a coleção completa de apólices de seguro cadastradas, " +
            "sem paginação.";
        Response<IReadOnlyList<ApoliceResponse>>(200, "Coleção de apólices");
    }
}
