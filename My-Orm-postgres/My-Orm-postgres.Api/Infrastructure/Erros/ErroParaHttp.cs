using Microsoft.AspNetCore.Http.HttpResults;
using My_Orm_postgres.Api.Common;

namespace My_Orm_postgres.Api.Infrastructure.Erros;

/// <summary>
/// Tradução de erros esperados (<see cref="Erro"/>) para respostas RFC 9457
/// (<c>application/problem+json</c>). É o único lugar que conhece a política de status HTTP.
/// </summary>
public static class ErroParaHttp
{
    /// <summary>
    /// Prefixo do membro <c>type</c>. O <c>type</c> completo é este prefixo seguido do código do
    /// erro e, como o código, nunca muda depois de publicado.
    /// </summary>
    public const string BaseTipos = "https://loja.example/problemas/";

    public const string CodigoErroInterno = "erro-interno";
    public const string CodigoValidacao = "validacao";

    public static int ParaStatusCode(this TipoErro tipo) => tipo switch
    {
        TipoErro.Validacao => StatusCodes.Status400BadRequest,
        TipoErro.NaoEncontrado => StatusCodes.Status404NotFound,
        TipoErro.Conflito => StatusCodes.Status409Conflict,
        TipoErro.RegraDeNegocio => StatusCodes.Status422UnprocessableEntity,
        TipoErro.Indisponivel => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Monta o ProblemDetails do erro. <c>instance</c> e <c>traceId</c> são preenchidos pelo
    /// <c>CustomizeProblemDetails</c> registrado em <c>AddProblemDetailsRfc9457</c>.
    /// </summary>
    public static ProblemHttpResult ParaProblema(this Erro erro)
    {
        var extensoes = new Dictionary<string, object?> { ["code"] = erro.Codigo };

        if (erro.Metadados is not null)
            foreach (var (chave, valor) in erro.Metadados)
                extensoes[chave] = valor;

        return TypedResults.Problem(
            statusCode: erro.Tipo.ParaStatusCode(),
            type: BaseTipos + erro.Codigo,
            title: erro.Titulo,
            detail: erro.Detalhe,
            extensions: extensoes);
    }
}
