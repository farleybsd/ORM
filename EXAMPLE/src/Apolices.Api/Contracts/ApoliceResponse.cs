namespace Apolices.Api.Contracts;

/// <param name="Versao">Versão atual do registro. Deve ser devolvida em PUT/DELETE (concorrência otimista).</param>
public sealed record ApoliceResponse(
    Guid Id,
    string Numero,
    SeguradoResponse Segurado,
    VigenciaResponse Vigencia,
    IReadOnlyList<CoberturaResponse> Coberturas,
    int Versao);

public sealed record SeguradoResponse(string Nome, string Documento);

public sealed record VigenciaResponse(DateOnly Inicio, DateOnly Fim);

public sealed record CoberturaResponse(string Tipo, decimal LimiteIndenizacao);
