using Apolices.Api.Common;
using Apolices.Api.Domain;

namespace Apolices.Api.Features.ExcluirApolice;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<Result> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var apolice = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (apolice is null)
            return Result.Failure(ErrosDeApolice.NaoEncontrada(request.Id));

        if (apolice.Versao != request.Versao)
            return Result.Failure(ErrosDeApolice.VersaoDesatualizada(request.Id, request.Versao));

        // Coberturas + apólice apagadas na mesma transação
        return await repository.RemoverAsync(request.Id, request.Versao, cancellationToken);
    }
}
