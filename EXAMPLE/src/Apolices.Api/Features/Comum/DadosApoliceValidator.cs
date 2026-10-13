using Apolices.Api.Contracts;
using FastEndpoints;
using FluentValidation;

namespace Apolices.Api.Features.Comum;

/// <summary>
/// Regras de formato comuns à criação e à atualização. Os limites batem com o tamanho das colunas
/// no banco, para que o erro seja um 400 claro e não uma exceção do driver.
/// </summary>
public abstract class DadosApoliceValidator<TRequest> : Validator<TRequest> where TRequest : DadosApoliceRequest
{
    private const int TamanhoMaximoNumero = 30;
    private const int TamanhoMaximoNome = 150;
    private const int TamanhoMaximoDocumento = 20;

    protected DadosApoliceValidator()
    {
        RuleFor(r => r.Numero)
            .NotEmpty().WithMessage("O número da apólice é obrigatório.")
            .MaximumLength(TamanhoMaximoNumero).WithMessage($"O número da apólice deve ter no máximo {TamanhoMaximoNumero} caracteres.");

        RuleFor(r => r.Segurado.Nome)
            .NotEmpty().WithMessage("O nome do segurado é obrigatório.")
            .MaximumLength(TamanhoMaximoNome).WithMessage($"O nome do segurado deve ter no máximo {TamanhoMaximoNome} caracteres.");

        RuleFor(r => r.Segurado.Documento)
            .NotEmpty().WithMessage("O documento do segurado é obrigatório.")
            .MaximumLength(TamanhoMaximoDocumento).WithMessage($"O documento do segurado deve ter no máximo {TamanhoMaximoDocumento} caracteres.");

        RuleFor(r => r.Vigencia.Inicio)
            .NotEqual(default(DateOnly)).WithMessage("O início da vigência é obrigatório.");

        RuleFor(r => r.Vigencia.Fim)
            .NotEqual(default(DateOnly)).WithMessage("O fim da vigência é obrigatório.")
            .GreaterThanOrEqualTo(r => r.Vigencia.Inicio).WithMessage("O fim da vigência não pode ser anterior ao início.");

        RuleFor(r => r.Coberturas)
            .NotEmpty().WithMessage("Informe ao menos uma cobertura.");

        RuleForEach(r => r.Coberturas).ChildRules(cobertura =>
        {
            cobertura.RuleFor(c => c.Tipo).IsInEnum().WithMessage("Tipo de cobertura inválido.");
            cobertura.RuleFor(c => c.LimiteIndenizacao)
                .GreaterThan(0).WithMessage("O limite de indenização deve ser maior que zero.");
        });
    }
}
