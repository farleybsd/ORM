namespace Sinistros.Api.Infrastructure.ApolicesApi;

/// <summary>
/// Espelha o contrato JSON exposto por GET /apolices/{id} na API de Apólices. É o único ponto
/// onde o formato externo é conhecido; o restante do contexto de Sinistros trabalha com
/// <see cref="Sinistros.Api.Domain.ApoliceReferenciada"/>.
/// </summary>
public sealed record ApoliceApiDto(
    Guid Id,
    string Numero,
    SeguradoApiDto Segurado,
    VigenciaApiDto Vigencia,
    IReadOnlyList<CoberturaApiDto> Coberturas);

public sealed record SeguradoApiDto(string Nome, string Documento);

public sealed record VigenciaApiDto(DateOnly Inicio, DateOnly Fim);

public sealed record CoberturaApiDto(string Tipo, decimal LimiteIndenizacao);
