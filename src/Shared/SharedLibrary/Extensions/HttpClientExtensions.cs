using System.Net.Http.Json;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Extensions;

/// <summary>
/// Provides extension methods for HttpClient to simplify JSON GET requests with graceful error handling.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Performs an HTTP GET request to fetch JSON data from the specified endpoint and deserializes it into the given type while handling errors gracefully.
    /// </summary>
    /// <typeparam name="T">The type into which the JSON payload is deserialized.</typeparam>
    /// <param name="client">The HttpClient instance used for making the request.</param>
    /// <param name="requestUri">The URI of the HTTP GET request.</param>
    /// <param name="logger">The logger instance for logging errors.</param>
    /// <param name="failureMessage">A custom failure message.</param>
    /// <returns>A Task that represents the asynchronous operation. The task result contains a FluentResults Result with the deserialized object if successful, or a failure message.</returns>
    public static async Task<Result<T>> GetFromJsonSafeAsync<T>(
        this HttpClient client,
        string requestUri,
        ILogger logger,
        string failureMessage
    )
    {
        var response = await client.GetAsync(requestUri);
        if (!response.IsSuccessStatusCode)
        {
            await logger.LogUnsuccessfulHttpResponse(response);
            return response.ToFailedResult(failureMessage);
        }

        var data = await response.Content.ReadFromJsonAsync<T>();
        if (data is null || data.Equals(default(T)))
        {
            var message =
                $"Response content from {requestUri} was null or could not be deserialized.";
            return Result.Fail(message);
        }

        return Result.Ok(data);
    }
}
