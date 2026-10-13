using Sinistros.Api.Common;

namespace Sinistros.Api.Domain;

public sealed class Sinistro
{
    public Guid Id { get; }
    public Guid ApoliceId { get; }
    public DateOnly DataOcorrencia { get; }
    public TipoCobertura TipoCobertura { get; }
    public string Descricao { get; private set; }
    public StatusSinistro Status { get; }
    public DateTime DataRegistro { get; }

    /// <summary>
    /// Versão para concorrência otimista: 0 enquanto não foi gravado; o repositório atualiza a
    /// cada gravação. O cliente devolve a versão que leu e a gravação falha se ela mudou.
    /// </summary>
    public int Versao { get; internal set; }

    private Sinistro(
        Guid id,
        Guid apoliceId,
        DateOnly dataOcorrencia,
        TipoCobertura tipoCobertura,
        string descricao,
        StatusSinistro status,
        DateTime dataRegistro,
        int versao)
    {
        Id = id;
        ApoliceId = apoliceId;
        DataOcorrencia = dataOcorrencia;
        TipoCobertura = tipoCobertura;
        Descricao = descricao;
        Status = status;
        DataRegistro = dataRegistro;
        Versao = versao;
    }

    /// <summary>
    /// Registra um sinistro validando, a partir dos dados já consultados da API de Apólices
    /// (traduzidos para <see cref="ApoliceReferenciada"/>), que a apólice estava vigente na data
    /// da ocorrência e que possui a cobertura solicitada.
    /// </summary>
    public static Result<Sinistro> Registrar(
        Guid apoliceId,
        DateOnly dataOcorrencia,
        TipoCobertura tipoCobertura,
        string descricao,
        ApoliceReferenciada apolice)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return ErrosDeSinistro.DescricaoObrigatoria;

        if (!apolice.EstaVigenteEm(dataOcorrencia))
            return ErrosDeSinistro.ApoliceForaDeVigencia(dataOcorrencia, apolice.VigenciaInicio, apolice.VigenciaFim);

        if (!apolice.PossuiCobertura(tipoCobertura))
            return ErrosDeSinistro.CoberturaNaoContratada(tipoCobertura);

        var sinistro = new Sinistro(
            Guid.NewGuid(),
            apoliceId,
            dataOcorrencia,
            tipoCobertura,
            descricao,
            StatusSinistro.Registrado,
            DateTime.UtcNow,
            versao: 0);

        return Result.Success(sinistro);
    }

    /// <summary>
    /// Recria o sinistro a partir de dados já persistidos (e portanto já validados).
    /// Uso exclusivo dos repositórios.
    /// </summary>
    internal static Sinistro Reconstituir(
        Guid id,
        Guid apoliceId,
        DateOnly dataOcorrencia,
        TipoCobertura tipoCobertura,
        string descricao,
        StatusSinistro status,
        DateTime dataRegistro,
        int versao)
        => new(id, apoliceId, dataOcorrencia, tipoCobertura, descricao, status, dataRegistro, versao);

    public Result AtualizarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return Result.Failure(ErrosDeSinistro.DescricaoObrigatoria);

        Descricao = descricao;
        return Result.Success();
    }
}
