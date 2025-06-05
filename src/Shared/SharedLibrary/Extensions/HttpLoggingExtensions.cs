using Microsoft.Extensions.Logging;

namespace SharedLibrary.Extensions;

/// <summary>
/// Provides extension methods for logging HTTP response details,
/// particularly for unsuccessful HTTP responses.
/// </summary>
public static partial class HttpLoggingExtensions
{
    /// <summary>
    /// Logs details of an unsuccessful HTTP response, including the request URI,
    /// status code, reason phrase, and response content.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> instance used for logging.</param>
    /// <param name="response">The <see cref="HttpResponseMessage"/> containing the HTTP response
    /// to be logged.</param>
    /// <returns>A task representing the asynchronous logging operation.</returns>
    public static async Task LogUnsuccessfulHttpResponse(
        this ILogger logger,
        HttpResponseMessage response
    )
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        var requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "<no URI>";
        var errorMessage = $"Request to '{requestUri}' failed.";

        LogUnsuccessfulResponse(
            logger,
            errorMessage,
            response.StatusCode,
            response.ReasonPhrase,
            errorContent
        );
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "{ErrorMessage} Status Code: {StatusCode}, Reason: {ReasonPhrase}, Content: {Content}"
    )]
    private static partial void LogUnsuccessfulResponse(
        ILogger logger,
        string errorMessage,
        System.Net.HttpStatusCode statusCode,
        string? reasonPhrase,
        string content
    );
}
