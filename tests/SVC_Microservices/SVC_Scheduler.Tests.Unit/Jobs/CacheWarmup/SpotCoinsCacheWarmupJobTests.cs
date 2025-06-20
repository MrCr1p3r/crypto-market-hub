using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Extensions.Testing;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;
using SVC_Scheduler.Jobs.CacheWarmup;
using SVC_Scheduler.MicroserviceClients.SvcExternal;
using SVC_Scheduler.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

namespace SVC_Scheduler.Tests.Unit.Jobs.CacheWarmup;

public class SpotCoinsCacheWarmupJobTests
{
    private readonly Mock<ISvcExternalClient> _mockSvcExternalClient;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly FakeLogger<SpotCoinsCacheWarmupJob> _logger;
    private readonly SpotCoinsCacheWarmupJob _job;

    public SpotCoinsCacheWarmupJobTests()
    {
        _mockSvcExternalClient = new Mock<ISvcExternalClient>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _logger = new FakeLogger<SpotCoinsCacheWarmupJob>();

        _job = new SpotCoinsCacheWarmupJob(
            _mockSvcExternalClient.Object,
            _mockMessagePublisher.Object,
            _logger
        );

        // Reset static state before each test
        ResetStaticState();
    }

    [Fact]
    public async Task Invoke_WhenFirstSuccessfulWarmup_ShouldPublishMessageAndLogFirstCompletion()
    {
        // Arrange
        var spotCoins = CreateTestSpotCoins(5);
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(spotCoins));

        // Act
        await _job.Invoke();

        // Assert
        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    "svc.scheduler.cache.warmup.completed",
                    It.Is<JobCompletedMessage>(msg =>
                        msg.JobName == "Spot Coins Cache Warmup" && msg.Success && msg.Data != null
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _logger.VerifyWasCalled(
            LogLevel.Information,
            "First cache warmup completed successfully with 5 coins - notification sent to GUI"
        );
        _logger.VerifyWasCalled(
            LogLevel.Information,
            "Cache warmup completion message published to GUI"
        );
    }

    [Fact]
    public async Task Invoke_WhenSubsequentSuccessfulWarmup_ShouldNotPublishMessage()
    {
        // Arrange - Simulate first warmup already completed
        var spotCoins = CreateTestSpotCoins(3);
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(spotCoins));

        // Act - First call
        await _job.Invoke();

        // Reset mocks for second call
        _mockMessagePublisher.Reset();

        // Act - Second call
        await _job.Invoke();

        // Assert
        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<JobCompletedMessage>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_WhenSuccessful_ShouldLogCorrectMessages()
    {
        // Arrange
        var spotCoins = CreateTestSpotCoins(10);
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(spotCoins));

        // Act
        await _job.Invoke();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Information, "SpotCoinsCacheWarmupJob started");
        _logger.VerifyWasCalled(
            LogLevel.Information,
            "Successfully retrieved 10 spot coins for cache warmup"
        );
        _logger.VerifyWasCalled(LogLevel.Information, "SpotCoinsCacheWarmupJob completed");
    }

    [Fact]
    public async Task Invoke_WhenFailed_ShouldLogErrorAndNotPublishMessage()
    {
        // Arrange
        var error = Result.Fail("External service unavailable");
        _mockSvcExternalClient.Setup(client => client.GetAllSpotCoins()).ReturnsAsync(error);

        // Act
        await _job.Invoke();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Information, "SpotCoinsCacheWarmupJob started");
        _logger.VerifyWasCalled(LogLevel.Warning, "Failed to retrieve spot coins for cache warmup");
        _logger.VerifyWasCalled(LogLevel.Information, "SpotCoinsCacheWarmupJob completed");

        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<JobCompletedMessage>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Invoke_WhenFirstWarmupSucceedsWithEmptyResult_ShouldStillPublishMessage()
    {
        // Arrange
        var emptySpotCoins = Array.Empty<Coin>();
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(emptySpotCoins.AsEnumerable()));

        // Act
        await _job.Invoke();

        // Assert
        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    It.IsAny<string>(),
                    It.Is<JobCompletedMessage>(msg => msg.Success && msg.Data != null),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _logger.VerifyWasCalled(
            LogLevel.Information,
            "First cache warmup completed successfully with 0 coins"
        );
    }

    [Fact]
    public async Task Invoke_MultipleSimultaneousCalls_ShouldOnlyPublishMessageOnce()
    {
        // Arrange
        var spotCoins = CreateTestSpotCoins(5);
        _mockSvcExternalClient
            .Setup(client => client.GetAllSpotCoins())
            .ReturnsAsync(Result.Ok(spotCoins));

        // Act - Simulate multiple simultaneous calls
        var tasks = new[] { _job.Invoke(), _job.Invoke(), _job.Invoke() };

        await Task.WhenAll(tasks);

        // Assert - Only one message should be published despite multiple calls
        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<JobCompletedMessage>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    #region Helper Methods

    private static IEnumerable<Coin> CreateTestSpotCoins(int count)
    {
        return Enumerable
            .Range(1, count)
            .Select(i => new Coin
            {
                Symbol = $"COIN{i}",
                Name = $"Test Coin {i}",
                TradingPairs = [],
            });
    }

    private static void ResetStaticState()
    {
        // Use reflection to reset the static field for clean test isolation
        var field = typeof(SpotCoinsCacheWarmupJob).GetField(
            "_firstWarmupCompleted",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        field?.SetValue(null, false);
    }

    #endregion
}
