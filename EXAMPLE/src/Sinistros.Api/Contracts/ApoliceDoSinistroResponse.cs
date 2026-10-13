namespace Sinistros.Api.Contracts;

/// <summary>
/// Dados da apólice vinculada a um sinistro, obtidos em tempo real da API de Apólices e já
/// traduzidos para o vocabulário deste contexto. Os dois últimos campos são conclusões do
/// domínio de Sinistros sobre a apólice (não vêm prontos da API externa).
/// </summary>
public sealed record ApoliceDoSinistroResponse(
    Guid SinistroId,
    Guid ApoliceId,
    DateOnly DataOcorrencia,
    string TipoCobertura,
    DateOnly VigenciaInicio,
    DateOnly VigenciaFim,
    IReadOnlyCollection<string> Coberturas,
    bool VigenteNaDataOcorrencia,
    bool PossuiCoberturaDoSinistro);
