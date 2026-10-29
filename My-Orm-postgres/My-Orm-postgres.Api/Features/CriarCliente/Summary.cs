using FastEndpoints;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;
using My_Orm_postgres.Api.Contracts;

namespace My_Orm_postgres.Api.Features.CriarCliente;

public sealed class Summary : Summary<Endpoint, Request>
{
    public Summary()
    {
        Summary = "Cadastra um novo cliente";
        Description = "Cria um cliente. O e-mail é único: não pode haver dois clientes com o mesmo e-mail.";
        RequestParam(r => r.Nome, "Nome do cliente, até 150 caracteres");
        RequestParam(r => r.Email, "E-mail do cliente, único, até 200 caracteres");
        RequestParam(r => r.Ativo, "Se o cliente está ativo (padrão: true)");
        Response<ClienteResponse>(201, "Cliente cadastrado com sucesso");
        Response<ProblemDetails>(400, "Um ou mais campos são inválidos (code: validacao)", "application/problem+json");
        Response<ProblemDetails>(409, "Já existe um cliente com o e-mail informado (code: cliente-email-ja-cadastrado)", "application/problem+json");
    }
}
