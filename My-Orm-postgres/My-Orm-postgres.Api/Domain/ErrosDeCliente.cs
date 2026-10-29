using My_Orm_postgres.Api.Common;

namespace My_Orm_postgres.Api.Domain;

/// <summary>
/// Catálogo dos erros esperados do contexto de Clientes. Cada <see cref="Erro.Codigo"/> é
/// publicado para os consumidores (aparece no <c>type</c> e no <c>code</c> do ProblemDetails) e
/// não pode ser renomeado: mudou o significado, crie um código novo e mantenha o antigo.
/// </summary>
public static class ErrosDeCliente
{
    public static Erro ClienteNaoEncontrado(long clienteId) => new(
        Codigo: "cliente-nao-encontrado",
        Titulo: "Cliente não encontrado",
        Detalhe: $"Nenhum cliente foi encontrado com o Id '{clienteId}'.",
        Tipo: TipoErro.NaoEncontrado,
        Metadados: new Dictionary<string, object?> { ["clienteId"] = clienteId });

    public static Erro EmailJaCadastrado(string email) => new(
        Codigo: "cliente-email-ja-cadastrado",
        Titulo: "E-mail já cadastrado",
        Detalhe: $"Já existe um cliente cadastrado com o e-mail '{email}'.",
        Tipo: TipoErro.Conflito,
        Metadados: new Dictionary<string, object?> { ["email"] = email });

    /// <summary>
    /// Concorrência otimista: a versão enviada pelo cliente não é mais a atual, porque outro
    /// usuário alterou (ou excluiu) o registro depois que ele foi lido.
    /// </summary>
    public static Erro VersaoDesatualizada(long clienteId) => new(
        Codigo: "cliente-versao-desatualizada",
        Titulo: "Cliente alterado por outro usuário",
        Detalhe: $"O cliente '{clienteId}' foi alterado depois da sua leitura. Consulte novamente e reenvie com a versão atual.",
        Tipo: TipoErro.Conflito,
        Metadados: new Dictionary<string, object?> { ["clienteId"] = clienteId });
}
