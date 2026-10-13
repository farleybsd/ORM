using Sinistros.Api.Common;

namespace Sinistros.Api.Domain;

/// <summary>
/// Catálogo dos erros esperados do contexto de Sinistros. Cada <see cref="Erro.Codigo"/> é
/// publicado para os consumidores (aparece no <c>type</c> e no <c>code</c> do ProblemDetails) e
/// não pode ser renomeado: mudou o significado, crie um código novo e mantenha o antigo.
/// </summary>
public static class ErrosDeSinistro
{
    public static Erro SinistroNaoEncontrado(Guid sinistroId) => new(
        Codigo: "sinistro-nao-encontrado",
        Titulo: "Sinistro não encontrado",
        Detalhe: $"Nenhum sinistro foi encontrado com o Id '{sinistroId}'.",
        Tipo: TipoErro.NaoEncontrado,
        Metadados: new Dictionary<string, object?> { ["sinistroId"] = sinistroId });

    public static Erro ApoliceNaoEncontrada(Guid apoliceId) => new(
        Codigo: "apolice-nao-encontrada",
        Titulo: "Apólice não encontrada",
        Detalhe: $"A apólice '{apoliceId}' não foi encontrada na API de Apólices.",
        Tipo: TipoErro.NaoEncontrado,
        Metadados: new Dictionary<string, object?> { ["apoliceId"] = apoliceId });

    public static Erro ApoliceForaDeVigencia(DateOnly dataOcorrencia, DateOnly vigenciaInicio, DateOnly vigenciaFim) => new(
        Codigo: "apolice-fora-de-vigencia",
        Titulo: "Apólice fora de vigência",
        Detalhe: $"A apólice não estava vigente em {dataOcorrencia:yyyy-MM-dd}.",
        Tipo: TipoErro.RegraDeNegocio,
        Metadados: new Dictionary<string, object?>
        {
            ["dataOcorrencia"] = dataOcorrencia,
            ["vigenciaInicio"] = vigenciaInicio,
            ["vigenciaFim"] = vigenciaFim
        });

    public static Erro CoberturaNaoContratada(TipoCobertura tipoCobertura) => new(
        Codigo: "cobertura-nao-contratada",
        Titulo: "Cobertura não contratada",
        Detalhe: $"A apólice não possui a cobertura '{tipoCobertura}'.",
        Tipo: TipoErro.RegraDeNegocio,
        Metadados: new Dictionary<string, object?> { ["tipoCobertura"] = tipoCobertura.ToString() });

    /// <summary>
    /// Trava otimista: o sinistro foi alterado ou excluído por outra requisição depois que o
    /// cliente o leu. O cliente deve consultar de novo e reaplicar a alteração.
    /// </summary>
    public static Erro VersaoDesatualizada(Guid sinistroId, int versaoInformada) => new(
        Codigo: "sinistro-versao-desatualizada",
        Titulo: "Sinistro alterado por outra requisição",
        Detalhe: $"O sinistro '{sinistroId}' foi alterado depois da versão {versaoInformada}. Consulte novamente e repita a operação.",
        Tipo: TipoErro.Conflito,
        Metadados: new Dictionary<string, object?> { ["sinistroId"] = sinistroId, ["versaoInformada"] = versaoInformada });

    public static readonly Erro DescricaoObrigatoria = new(
        Codigo: "sinistro-descricao-obrigatoria",
        Titulo: "Descrição do sinistro obrigatória",
        Detalhe: "A descrição do sinistro é obrigatória.",
        Tipo: TipoErro.Validacao);

    /// <summary>
    /// A API de Apólices não respondeu (timeout, circuito aberto, falha de rede ou 5xx). O motivo
    /// técnico fica só no log: o cliente recebe um texto fixo e pode tentar novamente mais tarde.
    /// </summary>
    public static readonly Erro ApolicesApiIndisponivel = new(
        Codigo: "apolices-api-indisponivel",
        Titulo: "API de Apólices indisponível",
        Detalhe: "Não foi possível consultar a API de Apólices no momento. Tente novamente em instantes.",
        Tipo: TipoErro.Indisponivel);
}
