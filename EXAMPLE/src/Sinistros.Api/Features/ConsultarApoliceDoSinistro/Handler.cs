using Sinistros.Api.Common;
using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.ConsultarApoliceDoSinistro;

/// <summary>
/// Dado um sinistro já registrado, consulta a API de Apólices (<c>GET /apolices/{id}</c>) para
/// devolver a apólice vinculada a ele. A leitura é feita sempre ao vivo: este contexto guarda
/// apenas o identificador da apólice, nunca uma cópia dos dados do contexto de Apólices.
/// </summary>
public sealed class Handler(
    ISinistroRepository repository,
    IApolicesApiClient apolicesApiClient,
    ILogger<Handler> logger)
{
    public async Task<Result<ApoliceDoSinistroResponse>> HandleAsync(Guid sinistroId, CancellationToken cancellationToken)
    {
        var sinistro = await repository.ObterPorIdAsync(sinistroId, cancellationToken);
        if (sinistro is null)
            return ErrosDeSinistro.SinistroNaoEncontrado(sinistroId);

        var consulta = await apolicesApiClient.ConsultarApoliceAsync(sinistro.ApoliceId, cancellationToken);

        return consulta switch
        {
            ApoliceConsultaResultado.Encontrada encontrada => Result.Success(Mapear(sinistro, encontrada.Apolice)),

            ApoliceConsultaResultado.NaoEncontrada => LogEApoliceNaoEncontrada(sinistro),

            ApoliceConsultaResultado.Indisponivel indisponivel => LogEIndisponivel(sinistro, indisponivel),

            _ => throw new InvalidOperationException("Resultado de consulta de apólice não tratado.")
        };
    }

    private Result<ApoliceDoSinistroResponse> LogEApoliceNaoEncontrada(Sinistro sinistro)
    {
        logger.LogWarning(
            "O sinistro {SinistroId} referencia a apólice {ApoliceId}, que não existe mais na API de Apólices.",
            sinistro.Id,
            sinistro.ApoliceId);

        return ErrosDeSinistro.ApoliceNaoEncontrada(sinistro.ApoliceId);
    }

    private Result<ApoliceDoSinistroResponse> LogEIndisponivel(Sinistro sinistro, ApoliceConsultaResultado.Indisponivel indisponivel)
    {
        logger.LogWarning(
            "Apólice do sinistro {SinistroId} não consultada: API de Apólices indisponível. Motivo: {Motivo}",
            sinistro.Id,
            indisponivel.Motivo);

        return ErrosDeSinistro.ApolicesApiIndisponivel;
    }

    private static ApoliceDoSinistroResponse Mapear(Sinistro sinistro, ApoliceReferenciada apolice) => new(
        sinistro.Id,
        apolice.ApoliceId,
        sinistro.DataOcorrencia,
        sinistro.TipoCobertura.ToString(),
        apolice.VigenciaInicio,
        apolice.VigenciaFim,
        apolice.Coberturas.Select(c => c.ToString()).ToList(),
        apolice.EstaVigenteEm(sinistro.DataOcorrencia),
        apolice.PossuiCobertura(sinistro.TipoCobertura));
}
