using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using SVC_External.Infrastructure.CoinGecko;
using static SVC_External.ExternalClients.MarketDataProviders.CoinGecko.CoinGeckoDtos;

namespace SVC_External.ExternalClients.MarketDataProviders.CoinGecko;

/// <summary>
/// Custom HTTP handler for CoinGecko API that provides adaptive authentication and rate limiting.
/// Detects monthly limit exhaustion (error code 10006) and automatically falls back to basic mode.
/// </summary>
public partial class CoinGeckoAdaptiveHandler(
    ICoinGeckoAuthenticationStateService authStateService,
    ILogger<CoinGeckoAdaptiveHandler> logger
) : DelegatingHandler
{
    private const int MonthlyLimitErrorCode = 10006;
    private readonly ICoinGeckoAuthenticationStateService _authStateService = authStateService;
    private readonly ILogger<CoinGeckoAdaptiveHandler> _logger = logger;

    private static readonly SlidingWindowRateLimiter _apiKeyRateLimiter = new(
        new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(60),
            SegmentsPerWindow = 20,
            PermitLimit = 30,
            QueueLimit = int.MaxValue,
        }
    );

    private static readonly SlidingWindowRateLimiter _basicRateLimiter = new(
        new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(12),
            SegmentsPerWindow = 120,
            PermitLimit = 1,
            QueueLimit = int.MaxValue,
        }
    );

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Apply current authentication headers
        ApplyCurrentAuthenticationHeaders(request);

        // Acquire rate limit permit
        var useApiKey = _authStateService.IsUsingApiKey;
        var rateLimiter = GetRateLimiter(useApiKey);
        using var lease = await rateLimiter.AcquireAsync(cancellationToken: cancellationToken);

        if (!lease.IsAcquired)
        {
            Logging.LogRateLimitExceeded(_logger);
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("Rate limit exceeded", Encoding.UTF8, "text/plain"),
            };
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Monthly limits only apply when using API key
        if (!useApiKey)
        {
            return response;
        }

        // Check for monthly limit error
        if (await IsMonthlyLimitExceeded(response))
        {
            Logging.LogMonthlyLimitExceeded(_logger, MonthlyLimitErrorCode);

            // Switch to basic mode
            _authStateService.SwitchToBasicMode();

            Logging.LogRetryingRequestInBasicMode(_logger);

            // Create a new request without API key
            var retryRequest = await CloneRequestAsync(request);
            ApplyCurrentAuthenticationHeaders(retryRequest);

            // Acquire permit from basic rate limiter
            var basicRateLimiter = GetRateLimiter(_authStateService.IsUsingApiKey);
            using var basicLease = await basicRateLimiter.AcquireAsync(
                cancellationToken: cancellationToken
            );

            if (basicLease.IsAcquired)
            {
                response.Dispose(); // Dispose the failed response
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    /// <summary>
    /// Gets the appropriate rate limiter based on the specified authentication mode.
    /// </summary>
    /// <param name="useApiKey">True to use API key rate limiter, false to use basic rate limiter.</param>
    /// <returns>The rate limiter to use for API calls.</returns>
    private static SlidingWindowRateLimiter GetRateLimiter(bool useApiKey) =>
        useApiKey ? _apiKeyRateLimiter : _basicRateLimiter;

    private void ApplyCurrentAuthenticationHeaders(HttpRequestMessage request)
    {
        // Remove existing API key header if present
        request.Headers.Remove("x-cg-demo-api-key");

        // Add API key header if currently using API key mode
        var apiKey = _authStateService.CurrentApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("x-cg-demo-api-key", apiKey);
        }
    }

    private async Task<bool> IsMonthlyLimitExceeded(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return false;
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            var errorResponse = JsonSerializer.Deserialize<CoinGeckoErrorResponse>(content);

            return errorResponse?.Status?.ErrorCode == MonthlyLimitErrorCode;
        }
        catch (JsonException ex)
        {
            Logging.LogFailedToParseErrorResponse(_logger, ex);
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clonedRequest = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copy headers
        foreach (var header in request.Headers)
        {
            clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            clonedRequest.Content = new StringContent(
                content,
                Encoding.UTF8,
                request.Content.Headers.ContentType?.MediaType ?? "application/json"
            );

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clonedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clonedRequest;
    }

    private static partial class Logging
    {
        [LoggerMessage(
            EventId = 1001,
            Level = LogLevel.Warning,
            Message = "Rate limit exceeded for CoinGecko request"
        )]
        public static partial void LogRateLimitExceeded(ILogger logger);

        [LoggerMessage(
            EventId = 1002,
            Level = LogLevel.Warning,
            Message = "CoinGecko monthly limit exceeded (error code {ErrorCode}). Switching to basic mode"
        )]
        public static partial void LogMonthlyLimitExceeded(ILogger logger, int errorCode);

        [LoggerMessage(
            EventId = 1003,
            Level = LogLevel.Information,
            Message = "Retrying CoinGecko request in basic mode"
        )]
        public static partial void LogRetryingRequestInBasicMode(ILogger logger);

        [LoggerMessage(
            EventId = 1004,
            Level = LogLevel.Warning,
            Message = "Failed to parse CoinGecko error response"
        )]
        public static partial void LogFailedToParseErrorResponse(
            ILogger logger,
            Exception exception
        );
    }
}
