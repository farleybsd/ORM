using Apolices.Api.Contracts;
using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Apolices.Api.Features.CriarApolice;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Cria uma apólice";
        Description = "Cria a apólice e suas coberturas em uma única transação. " +
            "A resposta traz a versão (1), que deve ser enviada nas alterações seguintes.";
        Response<ApoliceResponse>(201, "Apólice criada");
        Response<ProblemDetails>(400, "Dados inválidos (code: validacao, apolice-cobertura-repetida)", "application/problem+json");
        Response<ProblemDetails>(409, "Já existe apólice com o número informado (code: apolice-numero-duplicado)", "application/problem+json");
        Response<ProblemDetails>(422, "Apólice sem cobertura (code: apolice-sem-cobertura)", "application/problem+json");
    }
}
