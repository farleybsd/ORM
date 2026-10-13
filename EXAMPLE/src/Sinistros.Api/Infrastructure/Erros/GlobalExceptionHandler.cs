using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Sinistros.Api.Infrastructure.Erros;

/// <summary>
/// Último recurso para erros inesperados (exceções não tratadas). A exceção completa vai para o
/// log; o cliente recebe apenas um 500 genérico com o <c>traceId</c> que liga a resposta ao log.
/// Mensagem e stack trace da exceção nunca são expostos.
/// </summary>
/// <remarks>
/// Handlers específicos (se houver) devem ser registrados antes deste: ele sempre retorna
/// <c>true</c>, então nenhum handler registrado depois dele chegaria a rodar.
/// </remarks>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", http.Request.Method, http.Request.Path);
        http.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = ErroParaHttp.BaseTipos + ErroParaHttp.CodigoErroInterno,
                Title = "Erro interno",
                Detail = "Erro inesperado. Informe o traceId ao suporte.",
                Extensions = { ["code"] = ErroParaHttp.CodigoErroInterno }
            }
        });
    }
}
