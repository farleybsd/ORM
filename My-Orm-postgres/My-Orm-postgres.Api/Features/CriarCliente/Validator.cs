using FastEndpoints;
using FluentValidation;

namespace My_Orm_postgres.Api.Features.CriarCliente;

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Nome)
            .NotEmpty()
            .WithMessage("O nome do cliente é obrigatório.")
            .MaximumLength(150)
            .WithMessage("O nome do cliente deve ter no máximo 150 caracteres.");

        RuleFor(r => r.Email)
            .NotEmpty()
            .WithMessage("O e-mail do cliente é obrigatório.")
            .EmailAddress()
            .WithMessage("O e-mail do cliente é inválido.")
            .MaximumLength(200)
            .WithMessage("O e-mail do cliente deve ter no máximo 200 caracteres.");
    }
}
