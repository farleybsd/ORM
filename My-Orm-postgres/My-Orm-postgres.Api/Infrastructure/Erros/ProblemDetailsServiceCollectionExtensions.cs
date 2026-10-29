using System.Diagnostics;

namespace My_Orm_postgres.Api.Infrastructure.Erros;

public static class ProblemDetailsServiceCollectionExtensions
{
    /// <summary>
    /// Registra o padrão de erros da API segundo a RFC 9457 (Problem Details for HTTP APIs):
    /// o serviço que escreve respostas <c>application/problem+json</c>, com <c>instance</c> e
    /// <c>traceId</c> em todo erro, e o <see cref="GlobalExceptionHandler"/> para erros inesperados.
    /// </summary>
    /// <remarks>
    /// Use em conjunto com <see cref="ProblemDetailsApplicationBuilderExtensions.UseProblemDetailsRfc9457"/>.
    /// </remarks>
    public static IServiceCollection AddProblemDetailsRfc9457(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problema = context.ProblemDetails;
                var http = context.HttpContext;

                problema.Instance ??= http.Request.Path;
                problema.Extensions.TryAdd("traceId", Activity.Current?.Id ?? http.TraceIdentifier);
            };
        });

        // Handlers específicos antes, o global por último.
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
