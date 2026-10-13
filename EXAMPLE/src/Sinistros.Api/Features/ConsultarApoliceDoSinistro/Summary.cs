using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Consulta a apólice vinculada a um sinistro";
        Description = "Busca em tempo real, na API de Apólices, os dados da apólice " +
            "associada ao sinistro e indica se ela estava vigente e possuía a cobertura " +
            "na data da ocorrência.";
        Params["id"] = "Identificador do sinistro";
        Response<ApoliceDoSinistroResponse>(200, "Dados da apólice vinculada ao sinistro");
        Response<ProblemDetails>(404, "Sinistro não encontrado (code: sinistro-nao-encontrado), ou a apólice vinculada não foi encontrada na API de Apólices (code: apolice-nao-encontrada)", "application/problem+json");
        Response<ProblemDetails>(503, "A API de Apólices está indisponível no momento (code: apolices-api-indisponivel)", "application/problem+json");
    }
}
