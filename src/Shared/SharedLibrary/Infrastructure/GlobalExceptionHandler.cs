using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Infrastructure;

/// <summary>
/// Handles global exceptions by generating standardized JSON responses.
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) 
    : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _environment = environment;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            ArgumentNullException or ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new
        {
            type = statusCode switch
            {
                StatusCodes.Status400BadRequest => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
                StatusCodes.Status404NotFound => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
                StatusCodes.Status500InternalServerError => "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                _ => "https://datatracker.ietf.org/doc/html/rfc9110"
            },
            title = exception.GetType().FullName,
            status = statusCode,
            detail = exception.Message,
            instance = httpContext.Request.Path.Value,
            stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
            timestamp = DateTime.UtcNow
        };

        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = statusCode;

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problemDetails,
            JsonOptions,
            cancellationToken);

        return true;
    }
}
