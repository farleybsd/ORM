using Apolices.Api.Common;
using Apolices.Api.Contracts;
using Apolices.Api.Domain;

namespace Apolices.Api.Features.CriarApolice;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<Result<ApoliceResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var criacao = Apolice.Criar(
            Guid.NewGuid(),   // id gerado pela aplicação: não depende do banco (identity/sequence) para existir
            request.Numero.Trim(),
            request.ParaSegurado(),
            request.ParaVigencia(),
            request.ParaCoberturas());

        if (criacao.IsFailure)
            return criacao.Error!;

        var apolice = criacao.Value;

        // Apólice + coberturas em uma única transação (ver OrmApoliceRepository)
        var gravacao = await repository.AdicionarAsync(apolice, cancellationToken);
        if (gravacao.IsFailure)
            return gravacao.Error!;

        return Result.Success(ConsultarApolice.Handler.Mapear(apolice));
    }
}
