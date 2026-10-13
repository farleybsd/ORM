using Apolices.Api.Contracts;
using Apolices.Api.Domain;

namespace Apolices.Api.Features.ListarApolices;

public sealed class Handler(IApoliceRepository repository)
{
    public async Task<IReadOnlyList<ApoliceResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var apolices = await repository.ObterTodasAsync(cancellationToken);
        return apolices.Select(ConsultarApolice.Handler.Mapear).ToList();
    }
}
