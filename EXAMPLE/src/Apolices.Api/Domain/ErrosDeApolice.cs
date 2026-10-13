using Apolices.Api.Common;

namespace Apolices.Api.Domain;

/// <summary>
/// Catálogo dos erros esperados do contexto de Apólices. Cada <see cref="Erro.Codigo"/> é
/// publicado para os consumidores (aparece no <c>type</c> e no <c>code</c> do ProblemDetails) e
/// não pode ser renomeado: mudou o significado, crie um código novo e mantenha o antigo.
/// </summary>
public static class ErrosDeApolice
{
    public static Erro NaoEncontrada(Guid apoliceId) => new(
        Codigo: "apolice-nao-encontrada",
        Titulo: "Apólice não encontrada",
        Detalhe: $"Nenhuma apólice foi encontrada com o Id '{apoliceId}'.",
        Tipo: TipoErro.NaoEncontrado,
        Metadados: new Dictionary<string, object?> { ["apoliceId"] = apoliceId });

    public static readonly Erro NumeroObrigatorio = new(
        Codigo: "apolice-numero-obrigatorio",
        Titulo: "Número da apólice obrigatório",
        Detalhe: "O número da apólice é obrigatório.",
        Tipo: TipoErro.Validacao);

    public static readonly Erro SemCobertura = new(
        Codigo: "apolice-sem-cobertura",
        Titulo: "Apólice sem cobertura",
        Detalhe: "A apólice deve ter ao menos uma cobertura.",
        Tipo: TipoErro.RegraDeNegocio);

    public static Erro CoberturaRepetida(TipoCobertura tipo) => new(
        Codigo: "apolice-cobertura-repetida",
        Titulo: "Cobertura repetida",
        Detalhe: $"A cobertura '{tipo}' foi informada mais de uma vez.",
        Tipo: TipoErro.Validacao,
        Metadados: new Dictionary<string, object?> { ["tipoCobertura"] = tipo.ToString() });

    public static Erro NumeroDuplicado(string numero) => new(
        Codigo: "apolice-numero-duplicado",
        Titulo: "Número de apólice já cadastrado",
        Detalhe: $"Já existe uma apólice com o número '{numero}'.",
        Tipo: TipoErro.Conflito,
        Metadados: new Dictionary<string, object?> { ["numero"] = numero });

    /// <summary>
    /// Trava otimista: a apólice foi alterada ou excluída por outra requisição depois que o
    /// cliente a leu. O cliente deve consultar de novo e reaplicar a alteração.
    /// </summary>
    public static Erro VersaoDesatualizada(Guid apoliceId, int versaoInformada) => new(
        Codigo: "apolice-versao-desatualizada",
        Titulo: "Apólice alterada por outra requisição",
        Detalhe: $"A apólice '{apoliceId}' foi alterada depois da versão {versaoInformada}. Consulte novamente e repita a operação.",
        Tipo: TipoErro.Conflito,
        Metadados: new Dictionary<string, object?> { ["apoliceId"] = apoliceId, ["versaoInformada"] = versaoInformada });
}
