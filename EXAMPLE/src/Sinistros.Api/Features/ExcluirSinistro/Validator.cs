using FastEndpoints;
using FluentValidation;

namespace Sinistros.Api.Features.ExcluirSinistro;

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Versao)
            .GreaterThan(0).WithMessage("Informe a versão do sinistro obtida na consulta (?versao=N).");
    }
}
