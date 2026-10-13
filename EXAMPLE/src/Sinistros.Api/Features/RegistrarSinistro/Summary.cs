using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using Sinistros.Api.Contracts;

namespace Sinistros.Api.Features.RegistrarSinistro;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Registra um novo sinistro";
        Description = "Cria um sinistro vinculado a uma apólice, validando em tempo real " +
            "junto à API de Apólices que ela estava vigente na data da ocorrência e que " +
            "possui a cobertura informada.";
        RequestParam(r => r.ApoliceId, "Identificador da apólice à qual o sinistro se refere");
        RequestParam(r => r.DataOcorrencia, "Data em que o sinistro ocorreu (não pode ser futura)");
        RequestParam(r => r.TipoCobertura, "Tipo de cobertura acionada (deve existir na apólice)");
        RequestParam(r => r.Descricao, "Descrição do ocorrido, até 500 caracteres");
        Response<SinistroResponse>(201, "Sinistro registrado com sucesso");
        Response<ProblemDetails>(404, "A apólice informada não foi encontrada na API de Apólices (code: apolice-nao-encontrada)", "application/problem+json");
        Response<ProblemDetails>(422, "A apólice não estava vigente na data (code: apolice-fora-de-vigencia) ou não possui a cobertura solicitada (code: cobertura-nao-contratada)", "application/problem+json");
        Response<ProblemDetails>(503, "A API de Apólices está indisponível no momento (code: apolices-api-indisponivel)", "application/problem+json");
    }
}
