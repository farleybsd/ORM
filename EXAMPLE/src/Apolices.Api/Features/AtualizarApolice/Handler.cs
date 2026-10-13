using Apolices.Api.Common;
using Apolices.Api.Contracts;
using Apolices.Api.Domain;

namespace Apolices.Api.Features.AtualizarApolice;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<Result<ApoliceResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var apolice = await repository.ObterPorIdAsync(request.Id, cancellationToken);
        if (apolice is null)
            return ErrosDeApolice.NaoEncontrada(request.Id);

        // Falha rápida: o cliente já está com dados velhos, nem tenta gravar.
        // A conferência que vale é a do banco (UPDATE ... WHERE versao = @versao), que cobre a corrida.
        if (apolice.Versao != request.Versao)
            return ErrosDeApolice.VersaoDesatualizada(request.Id, request.Versao);

        var atualizacao = apolice.Atualizar(
            request.Numero.Trim(),
            request.ParaSegurado(),
            request.ParaVigencia(),
            request.ParaCoberturas());

        if (atualizacao.IsFailure)
            return atualizacao.Error!;

        var gravacao = await repository.AtualizarAsync(apolice, request.Versao, cancellationToken);
        if (gravacao.IsFailure)
            return gravacao.Error!;

        return Result.Success(ConsultarApolice.Handler.Mapear(apolice));
    }
}
