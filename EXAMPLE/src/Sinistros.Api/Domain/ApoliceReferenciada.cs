namespace Sinistros.Api.Domain;

/// <summary>
/// Representação, no vocabulário de Sinistros, dos dados de uma apólice obtidos via HTTP da API
/// de Apólices. É o resultado da tradução do contrato externo (DTO) para os conceitos usados por
/// este contexto — nenhuma entidade ou tipo do contexto de Apólices é referenciado aqui.
/// </summary>
public sealed record ApoliceReferenciada(
    Guid ApoliceId,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    IReadOnlyCollection<TipoCobertura> Coberturas)
{
    public bool EstaVigenteEm(DateOnly data) => data >= VigenciaInicio && data <= VigenciaFim;

    public bool PossuiCobertura(TipoCobertura tipo) => Coberturas.Contains(tipo);
}
