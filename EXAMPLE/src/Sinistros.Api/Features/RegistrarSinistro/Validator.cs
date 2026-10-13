using FastEndpoints;
using FluentValidation;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.RegistrarSinistro;

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.ApoliceId)
            .NotEmpty()
            .WithMessage("O identificador da apólice é obrigatório.");

        RuleFor(r => r.DataOcorrencia)
            .NotEqual(default(DateOnly))
            .WithMessage("A data de ocorrência é obrigatória.")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A data de ocorrência não pode estar no futuro.");

        RuleFor(r => r.TipoCobertura)
            .NotEmpty()
            .WithMessage("O tipo de cobertura é obrigatório.")
            .Must(tipo => Enum.TryParse<TipoCobertura>(tipo, ignoreCase: true, out _))
            .WithMessage($"O tipo de cobertura deve ser um dos seguintes: {string.Join(", ", Enum.GetNames<TipoCobertura>())}.");

        RuleFor(r => r.Descricao)
            .NotEmpty()
            .WithMessage("A descrição do sinistro é obrigatória.")
            .MaximumLength(500)
            .WithMessage("A descrição do sinistro deve ter no máximo 500 caracteres.");
    }
}
