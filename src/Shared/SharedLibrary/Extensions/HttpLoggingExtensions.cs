using Microsoft.Extensions.Logging;

namespace SharedLibrary.Extensions;

/// <summary>
/// Provides extension methods for logging HTTP response details,
/// particularly for unsuccessful HTTP responses.
/// </summary>
public static class HttpLoggingExtensions
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

        logger.LogWarning(
            "{ErrorMessage} Status Code: {StatusCode}, Reason: {ReasonPhrase}, Content: {Content}",
            errorMessage,
            response.StatusCode,
            response.ReasonPhrase,
            errorContent
        );
    }
}
