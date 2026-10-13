using Sinistros.Api.Common;

namespace Sinistros.Api.Domain;

/// <summary>
/// Repositório do agregado Sinistro. Conflitos esperados (versão desatualizada) voltam como
/// <see cref="Result"/>, sem expor detalhes do banco ao domínio.
/// </summary>
public interface ISinistroRepository
{
    Task AdicionarAsync(Sinistro sinistro, CancellationToken cancellationToken);
    Task<Sinistro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Sinistro>> ListarPorApoliceAsync(Guid apoliceId, CancellationToken cancellationToken);

    /// <summary>Grava o sinistro se a versão no banco ainda for <paramref name="versaoEsperada"/>.</summary>
    Task<Result> AtualizarAsync(Sinistro sinistro, int versaoEsperada, CancellationToken cancellationToken);

    /// <summary>Exclui o sinistro se a versão no banco ainda for <paramref name="versaoEsperada"/>.</summary>
    Task<Result> RemoverAsync(Guid id, int versaoEsperada, CancellationToken cancellationToken);
}
