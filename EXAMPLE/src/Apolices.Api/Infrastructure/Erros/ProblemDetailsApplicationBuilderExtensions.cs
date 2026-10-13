using System.Diagnostics;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Apolices.Api.Infrastructure.Erros;

public static class ProblemDetailsApplicationBuilderExtensions
{
    /// <summary>
    /// Coloca no pipeline os middlewares que garantem que todo erro sai como
    /// <c>application/problem+json</c>: <c>UseExceptionHandler</c> (exceções não tratadas viram
    /// um 500 limpo) e <c>UseStatusCodePages</c> (erros que sairiam com corpo vazio, como o 404
    /// de rota inexistente, ganham um corpo ProblemDetails).
    /// </summary>
    /// <remarks>
    /// Deve ser chamado antes de <c>UseFastEndpoints</c>, para envolver a execução dos endpoints.
    /// </remarks>
    public static IApplicationBuilder UseProblemDetailsRfc9457(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }

    /// <summary>
    /// Faz os erros de validação do FastEndpoints seguirem o mesmo contrato dos demais erros:
    /// <see cref="HttpValidationProblemDetails"/> com <c>type</c>, <c>code</c>, <c>instance</c>
    /// e <c>traceId</c>, e os erros de cada campo em <c>errors</c>.
    /// </summary>
    public static void UseProblemDetailsRfc9457(this ErrorOptions errors)
    {
        errors.ContentType = "application/problem+json";
        errors.ProducesMetadataType = typeof(HttpValidationProblemDetails);
        errors.ResponseBuilder = (falhas, http, statusCode) => new HttpValidationProblemDetails(
            falhas
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()))
        {
            Status = statusCode,
            Type = ErroParaHttp.BaseTipos + ErroParaHttp.CodigoValidacao,
            Title = "Um ou mais campos são inválidos",
            Detail = "Corrija os campos listados em 'errors' e envie a requisição novamente.",
            Instance = http.Request.Path,
            Extensions =
            {
                ["code"] = ErroParaHttp.CodigoValidacao,
                ["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier
            }
        };
    }
}
