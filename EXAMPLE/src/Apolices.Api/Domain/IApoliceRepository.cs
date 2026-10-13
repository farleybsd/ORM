using Apolices.Api.Common;

namespace Apolices.Api.Domain;

/// <summary>
/// Repositório do agregado Apólice (apólice + coberturas). Toda gravação é atômica: ou o
/// agregado inteiro é gravado, ou nada é. Conflitos esperados (número duplicado, versão
/// desatualizada) voltam como <see cref="Result"/>, sem expor detalhes do banco ao domínio.
/// </summary>
public interface IApoliceRepository
{
    Task<Apolice?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Apolice>> ObterTodasAsync(CancellationToken cancellationToken);

    Task<Result> AdicionarAsync(Apolice apolice, CancellationToken cancellationToken);

    /// <summary>Grava a apólice se a versão no banco ainda for <paramref name="versaoEsperada"/>.</summary>
    Task<Result> AtualizarAsync(Apolice apolice, int versaoEsperada, CancellationToken cancellationToken);

    /// <summary>Exclui a apólice se a versão no banco ainda for <paramref name="versaoEsperada"/>.</summary>
    Task<Result> RemoverAsync(Guid id, int versaoEsperada, CancellationToken cancellationToken);
}
