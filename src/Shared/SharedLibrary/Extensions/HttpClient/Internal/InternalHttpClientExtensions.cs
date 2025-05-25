using System.Net.Http.Json;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Extensions.HttpClient.Internal;

/// <summary>
/// Provides extension methods for HttpClient to perform internal HTTP requests with graceful error handling.
/// </summary>
public static class InternalHttpClientExtensions
{
    /// <summary>
    /// Performs an HTTP GET request to fetch JSON data from the specified endpoint
    /// and deserializes it into the given type while handling errors gracefully.
    /// </summary>
    /// <typeparam name="T">The type into which the JSON payload is deserialized.</typeparam>
    /// <param name="client">The HttpClient instance used for making the request.</param>
    /// <param name="requestUri">The URI of the HTTP GET request.</param>
    /// <param name="logger">The logger instance for logging errors.</param>
    /// <param name="failureMessage">A custom failure message.</param>
    /// <returns>
    /// Success: Result with the deserialized object.
    /// Failure: Failure message.
    /// </returns>
    public static async Task<Result<T>> GetFromJsonSafeAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        ILogger logger,
        string failureMessage
    )
    {
        var response = await client.GetAsync(requestUri);
        return await ProcessHttpResponseAsync<T>(response, logger, failureMessage);
    }

    /// <summary>
    /// Performs an HTTP POST request with a JSON payload to the specified endpoint
    /// and deserializes the JSON response into the given type while handling errors
    /// gracefully.
    /// </summary>
    /// <typeparam name="TRequest">The type of the object to be serialized into the JSON payload of the request.</typeparam>
    /// <typeparam name="TResponse">The type into which the JSON payload of the response is deserialized.</typeparam>
    /// <param name="client">The HttpClient instance used for making the request.</param>
    /// <param name="requestUri">The URI of the HTTP POST request.</param>
    /// <param name="requestData">The data to be sent as JSON in the request body.</param>
    /// <param name="logger">The logger instance for logging errors.</param>
    /// <param name="failureMessage">A custom failure message.</param>
    /// <returns>
    /// Success: Result with the deserialized object.
    /// Failure: Failure message.
    /// </returns>
    public static async Task<Result<TResponse>> PostAsJsonSafeAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        TRequest requestData,
        ILogger logger,
        string failureMessage
    )
    {
        var response = await client.PostAsJsonAsync(requestUri, requestData);
        return await ProcessHttpResponseAsync<TResponse>(response, logger, failureMessage);
    }

    /// <summary>
    /// Performs an HTTP PATCH request with a JSON payload to the specified endpoint
    /// and deserializes the JSON response into the given type while handling errors
    /// gracefully.
    /// </summary>
    /// <typeparam name="TRequest">The type of the object to be serialized into the JSON payload of the request.</typeparam>
    /// <typeparam name="TResponse">The type into which the JSON payload of the response is deserialized.</typeparam>
    /// <param name="client">The HttpClient instance used for making the request.</param>
    /// <param name="requestUri">The URI of the HTTP PATCH request.</param>
    /// <param name="requestData">The data to be sent as JSON in the request body.</param>
    /// <param name="logger">The logger instance for logging errors.</param>
    /// <param name="failureMessage">A custom failure message.</param>
    /// <returns>
    /// Success: Result with the deserialized object.
    /// Failure: Failure message.
    /// </returns>
    public static async Task<Result<TResponse>> PatchAsJsonSafeAsync<TRequest, TResponse>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        TRequest requestData,
        ILogger logger,
        string failureMessage
    )
    {
        var response = await client.PatchAsJsonAsync(requestUri, requestData);
        return await ProcessHttpResponseAsync<TResponse>(response, logger, failureMessage);
    }

    private static async Task<Result<TResponse>> ProcessHttpResponseAsync<TResponse>(
        HttpResponseMessage response,
        ILogger logger,
        string failureMessage
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            await logger.LogUnsuccessfulHttpResponse(response);

            return await InternalHttpResponseExtensions.ToFailedResultAsync<TResponse>(
                response,
                failureMessage
            );
        }

        var content = await response.Content.ReadFromJsonAsync<TResponse>();
        return content is null || content.Equals(default(TResponse))
            ? ToFailedResult<TResponse>(response)
            : Result.Ok(content);
    }

    private static Result<TResponse> ToFailedResult<TResponse>(HttpResponseMessage response)
    {
        var httpMethod = response.RequestMessage!.Method;
        var requestUri = response.RequestMessage!.RequestUri!;
        string deserializationFailureMessage =
            $"Response content from [{httpMethod}]: {requestUri} was null or could not be deserialized.";
        return Result.Fail(deserializationFailureMessage);
    }
}
