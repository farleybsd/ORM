using Sinistros.Api.Common;
using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.AtualizarSinistro;

public sealed class Handler(ISinistroRepository repository)
{
    public async Task<Result<SinistroResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (sinistro is null)
            return ErrosDeSinistro.SinistroNaoEncontrado(request.Id);

        // Falha rápida: o cliente já está com dados velhos, nem tenta gravar.
        // A conferência que vale é a do banco (UPDATE ... WHERE versao = @versao), que cobre a corrida.
        if (sinistro.Versao != request.Versao)
            return ErrosDeSinistro.VersaoDesatualizada(request.Id, request.Versao);

        var alteracao = sinistro.AtualizarDescricao(request.Descricao.Trim());
        if (alteracao.IsFailure)
            return alteracao.Error!;

        var gravacao = await repository.AtualizarAsync(sinistro, request.Versao, cancellationToken);
        if (gravacao.IsFailure)
            return gravacao.Error!;

        return Result.Success(RegistrarSinistro.Handler.Mapear(sinistro));
    }
}
