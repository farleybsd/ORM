using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Apolices.Api.Features.ExcluirApolice;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Exclui uma apólice";
        Description = "Exclui a apólice e suas coberturas em uma única transação, desde que a " +
            "versão informada ainda seja a atual.";
        Params["id"] = "Identificador da apólice";
        RequestParam(r => r.Versao, "Versão da apólice obtida na última consulta (query string)");
        Response(204, "Apólice excluída");
        Response<ProblemDetails>(404, "Apólice não encontrada (code: apolice-nao-encontrada)", "application/problem+json");
        Response<ProblemDetails>(409, "Versão desatualizada (code: apolice-versao-desatualizada)", "application/problem+json");
    }
}
