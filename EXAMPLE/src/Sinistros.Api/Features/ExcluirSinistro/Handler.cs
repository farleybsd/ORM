using Sinistros.Api.Common;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.ExcluirSinistro;

public sealed class Handler(ISinistroRepository repository)
{
    public async Task<Result> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (sinistro is null)
            return Result.Failure(ErrosDeSinistro.SinistroNaoEncontrado(request.Id));

        if (sinistro.Versao != request.Versao)
            return Result.Failure(ErrosDeSinistro.VersaoDesatualizada(request.Id, request.Versao));

        return await repository.RemoverAsync(request.Id, request.Versao, cancellationToken);
    }
}
