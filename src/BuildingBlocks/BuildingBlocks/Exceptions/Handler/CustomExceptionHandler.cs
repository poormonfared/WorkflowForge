using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions.Handler;

public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new
        {
            title,
            status = statusCode,
            detail = exception.Message,
            errors = exception is ValidationException validationException
                ? validationException.Errors.Select(e => e.ErrorMessage)
                : null,
        }, cancellationToken);

        return true;
    }
}
