using FastEndpoints;
using FluentValidation;

namespace Sinistros.Api.Features.AtualizarSinistro;

public sealed class Validator : Validator<Request>
{
    private const int TamanhoMaximoDescricao = 500;

    public Validator()
    {
        RuleFor(r => r.Descricao)
            .NotEmpty().WithMessage("A descrição do sinistro é obrigatória.")
            .MaximumLength(TamanhoMaximoDescricao)
            .WithMessage($"A descrição do sinistro deve ter no máximo {TamanhoMaximoDescricao} caracteres.");

        RuleFor(r => r.Versao)
            .GreaterThan(0).WithMessage("Informe a versão do sinistro obtida na consulta.");
    }
}
