using Apolices.Api.Contracts;
using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Apolices.Api.Features.ConsultarApolice;

public sealed class Summary : Summary<Endpoint>
{
    public Summary()
    {
        Summary = "Consulta uma apólice pelo identificador";
        Description = "Retorna os dados completos de uma apólice de seguro " +
            "(segurado, vigência e coberturas) a partir do seu Id.";
        Params["id"] = "Identificador da apólice";
        Response<ApoliceResponse>(200, "Apólice encontrada");
        Response<ProblemDetails>(404, "Nenhuma apólice foi encontrada com o Id informado (code: apolice-nao-encontrada)", "application/problem+json");
    }
}
