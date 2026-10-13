using Apolices.Api.Contracts;
using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Apolices.Api.Features.AtualizarApolice;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Atualiza uma apólice";
        Description = "Substitui os dados e as coberturas da apólice em uma única transação. " +
            "Usa concorrência otimista: envie a 'versao' recebida na consulta; se a apólice " +
            "tiver sido alterada depois disso, a resposta é 409 e nada é gravado.";
        Params["id"] = "Identificador da apólice";
        RequestParam(r => r.Versao, "Versão da apólice obtida na última consulta");
        Response<ApoliceResponse>(200, "Apólice atualizada (com a nova versão)");
        Response<ProblemDetails>(400, "Dados inválidos (code: validacao, apolice-cobertura-repetida)", "application/problem+json");
        Response<ProblemDetails>(404, "Apólice não encontrada (code: apolice-nao-encontrada)", "application/problem+json");
        Response<ProblemDetails>(409, "Versão desatualizada (code: apolice-versao-desatualizada) ou número já usado (code: apolice-numero-duplicado)", "application/problem+json");
    }
}
