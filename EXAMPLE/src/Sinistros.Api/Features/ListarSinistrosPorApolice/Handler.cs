using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.ListarSinistrosPorApolice;

public sealed class Handler(ISinistroRepository repository)
{
    public async Task<IReadOnlyList<SinistroResponse>> HandleAsync(Guid apoliceId, CancellationToken cancellationToken)
    {
        var sinistros = await repository.ListarPorApoliceAsync(apoliceId, cancellationToken);
        return sinistros.Select(RegistrarSinistro.Handler.Mapear).ToList();
    }
}
