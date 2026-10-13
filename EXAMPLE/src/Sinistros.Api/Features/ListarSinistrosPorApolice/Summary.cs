using FastEndpoints;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ListarSinistrosPorApolice;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Lista os sinistros de uma apólice";
        Description = "Retorna todos os sinistros registrados para a apólice informada.";
        Params["apoliceId"] = "Identificador da apólice";
        Response<IReadOnlyList<SinistroResponse>>(200, "Coleção de sinistros da apólice (vazia se não houver nenhum)");
    }
}
