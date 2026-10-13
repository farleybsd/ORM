using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Sinistros.Api.Features.ExcluirSinistro;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Exclui um sinistro";
        Description = "Exclui o sinistro desde que a versão informada ainda seja a atual.";
        Params["id"] = "Identificador do sinistro";
        RequestParam(r => r.Versao, "Versão do sinistro obtida na última consulta (query string)");
        Response(204, "Sinistro excluído");
        Response<ProblemDetails>(404, "Sinistro não encontrado (code: sinistro-nao-encontrado)", "application/problem+json");
        Response<ProblemDetails>(409, "Versão desatualizada (code: sinistro-versao-desatualizada)", "application/problem+json");
    }
}
