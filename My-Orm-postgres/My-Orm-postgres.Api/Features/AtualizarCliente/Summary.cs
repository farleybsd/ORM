using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.AtualizarCliente;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Atualiza um cliente";
        Description = "Substitui os dados do cliente. Usa concorrência otimista: envie a 'versao' " +
            "recebida na consulta; se outro usuário alterou o cliente depois disso, a atualização é recusada.";
        Params["id"] = "Identificador do cliente";
        RequestParam(r => r.Nome, "Nome do cliente, até 150 caracteres");
        RequestParam(r => r.Email, "E-mail do cliente, único, até 200 caracteres");
        RequestParam(r => r.Ativo, "Se o cliente está ativo");
        RequestParam(r => r.Versao, "Versão do cliente recebida na consulta (campo 'versao')");
        Response<ClienteResponse>(200, "Cliente atualizado (a resposta traz a nova versão)");
        Response<ProblemDetails>(400, "Um ou mais campos são inválidos (code: validacao)", "application/problem+json");
        Response<ProblemDetails>(404, "Nenhum cliente foi encontrado com o Id informado (code: cliente-nao-encontrado)", "application/problem+json");
        Response<ProblemDetails>(409, "O e-mail já pertence a outro cliente (code: cliente-email-ja-cadastrado) ou a versão está desatualizada (code: cliente-versao-desatualizada)", "application/problem+json");
    }
}
