using Sinistros.Api.Common;
using Sinistros.Api.Contracts;
using Sinistros.Api.Domain;

namespace Sinistros.Api.Features.RegistrarSinistro;

public sealed class Handler(
    IApolicesApiClient apolicesApiClient,
    ISinistroRepository repository,
    ILogger<Handler> logger)
{
    public async Task<Result<SinistroResponse>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var consulta = await apolicesApiClient.ConsultarApoliceAsync(request.ApoliceId, cancellationToken);

        return consulta switch
        {
            ApoliceConsultaResultado.NaoEncontrada => ErrosDeSinistro.ApoliceNaoEncontrada(request.ApoliceId),

            ApoliceConsultaResultado.Indisponivel indisponivel => LogEIndisponivel(request.ApoliceId, indisponivel),

            ApoliceConsultaResultado.Encontrada encontrada => await RegistrarAsync(request, encontrada.Apolice, cancellationToken),

            _ => throw new InvalidOperationException("Resultado de consulta de apólice não tratado.")
        };
    }

    private Result<SinistroResponse> LogEIndisponivel(Guid apoliceId, ApoliceConsultaResultado.Indisponivel indisponivel)
    {
        logger.LogWarning(
            "Sinistro não registrado: API de Apólices indisponível para a apólice {ApoliceId}. Motivo: {Motivo}",
            apoliceId,
            indisponivel.Motivo);

        return ErrosDeSinistro.ApolicesApiIndisponivel;
    }

    private async Task<Result<SinistroResponse>> RegistrarAsync(Request request, ApoliceReferenciada apolice, CancellationToken cancellationToken)
    {
        var tipoCobertura = Enum.Parse<TipoCobertura>(request.TipoCobertura, ignoreCase: true);

        var registro = Sinistro.Registrar(request.ApoliceId, request.DataOcorrencia, tipoCobertura, request.Descricao, apolice);
        if (registro.IsFailure)
            return registro.Error!;

        var sinistro = registro.Value;
        await repository.AdicionarAsync(sinistro, cancellationToken);

        return Result.Success(Mapear(sinistro));
    }

    internal static SinistroResponse Mapear(Sinistro sinistro) => new(
        sinistro.Id,
        sinistro.ApoliceId,
        sinistro.DataOcorrencia,
        sinistro.TipoCobertura.ToString(),
        sinistro.Descricao,
        sinistro.Status.ToString(),
        sinistro.DataRegistro,
        sinistro.Versao);
}
