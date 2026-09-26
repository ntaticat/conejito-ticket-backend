using Microsoft.AspNetCore.Diagnostics;

namespace ConejitoTicket.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException e => (e.StatusCode, e.Message),
            ArgumentException e => (StatusCodes.Status400BadRequest, e.Message),
            UnauthorizedAccessException e => (StatusCodes.Status401Unauthorized, e.Message),
            KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado."),
        };

        if (status >= 500) logger.LogError(exception, "Excepción no controlada");

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title },
        });
    }
}
