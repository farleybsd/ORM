namespace Sinistros.Api.Contracts;

/// <param name="Versao">Versão atual do registro. Deve ser devolvida em PUT/DELETE (concorrência otimista).</param>
public sealed record SinistroResponse(
    Guid Id,
    Guid ApoliceId,
    DateOnly DataOcorrencia,
    string TipoCobertura,
    string Descricao,
    string Status,
    DateTime DataRegistro,
    int Versao);
