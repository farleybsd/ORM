using FastEndpoints;
using FluentValidation;

namespace Apolices.Api.Features.ExcluirApolice;

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Versao)
            .GreaterThan(0).WithMessage("Informe a versão da apólice obtida na consulta (?versao=N).");
    }
}
