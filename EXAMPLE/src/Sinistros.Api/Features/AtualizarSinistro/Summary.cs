using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.AtualizarSinistro;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Atualiza a descrição de um sinistro";
        Description = "Usa concorrência otimista: envie a 'versao' recebida na consulta; se o " +
            "sinistro tiver sido alterado depois disso, a resposta é 409 e nada é gravado.";
        Params["id"] = "Identificador do sinistro";
        RequestParam(r => r.Descricao, "Nova descrição, até 500 caracteres");
        RequestParam(r => r.Versao, "Versão do sinistro obtida na última consulta");
        Response<SinistroResponse>(200, "Sinistro atualizado (com a nova versão)");
        Response<ProblemDetails>(400, "Dados inválidos (code: validacao)", "application/problem+json");
        Response<ProblemDetails>(404, "Sinistro não encontrado (code: sinistro-nao-encontrado)", "application/problem+json");
        Response<ProblemDetails>(409, "Versão desatualizada (code: sinistro-versao-desatualizada)", "application/problem+json");
    }
}
