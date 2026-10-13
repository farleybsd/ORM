using Apolices.Api.Features.Comum;
using FluentValidation;

namespace Apolices.Api.Features.AtualizarApolice;

public sealed class Validator : DadosApoliceValidator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Versao)
            .GreaterThan(0).WithMessage("Informe a versão da apólice obtida na consulta.");
    }
}
