using GUI_Crypto.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Constants;

namespace GUI_Crypto.Tests.Integration.Messaging;

/// <summary>
/// Integration tests for CacheWarmupMessageHandler testing the full messaging pipeline:
/// RabbitMQ Message → MessageHandler → SignalR Hub → Connected Clients.
/// Also tests server-side cache warmup state management and immediate notifications.
/// </summary>
[Collection("Messaging Integration Tests")]
public class CacheWarmupMessageIntegrationTest(CustomWebApplicationFactory factory)
    : BaseMessagingIntegrationTest(factory)
{
    /// <summary>
    /// Resets the cache warmup state service to ensure test isolation.
    /// This creates a new instance since the service is registered as singleton.
    /// </summary>
    private void ResetCacheWarmupState()
    {
        // Get the service and reset it if it has a reset method, or use reflection
        using var scope = Factory.Services.CreateScope();
        var stateService = scope.ServiceProvider.GetRequiredService<ICacheWarmupStateService>();

        // Since we're using a simple boolean field, we can use reflection to reset it
        if (stateService is CacheWarmupStateService concreteService)
        {
            var field = typeof(CacheWarmupStateService).GetField(
                "_isWarmedUp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            field?.SetValue(concreteService, false);
        }
    }

    [Fact]
    public async Task CacheWarmupMessage_ShouldNotifyAllSignalRClients()
    {
        // Arrange
        var connection = await GetSignalRConnection();
        var listener = CreateParameterlessSignalRListener(
            connection,
            "ReceiveCacheWarmupCompleted"
        );

        // Act - Publish cache warmup completion message
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiCacheWarmupCompleted,
            new { CoinCount = 100, IsFirstWarmup = true }
        );

        // Wait for the message to be received
        await listener.WaitAsync();

        // Assert
        listener
            .CallCount.Should()
            .Be(1, "exactly one cache warmup notification should be received");
    }

    [Fact]
    public async Task CacheWarmupMessage_WithMultipleClients_ShouldNotifyAllClients()
    {
        // Arrange
        const int clientCount = 3;
        var connections = (await GetMultipleSignalRConnections(clientCount)).ToList();
        var listeners = connections
            .Select(c => CreateParameterlessSignalRListener(c, "ReceiveCacheWarmupCompleted"))
            .ToList();

        // Act
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiCacheWarmupCompleted,
            new { CoinCount = 50 }
        );

        // Wait for all clients to receive the notification
        await Task.WhenAll(listeners.Select(l => l.WaitAsync()));

        // Assert - All clients should receive the notification
        listeners
            .Should()
            .AllSatisfy(listener =>
                listener
                    .CallCount.Should()
                    .Be(1, "each client should receive exactly one notification")
            );
    }

    [Fact]
    public async Task CacheWarmupMessage_ShouldUpdateServerSideState()
    {
        // Arrange - Reset state for test isolation
        ResetCacheWarmupState();

        // Get the server-side state service
        using var scope = Factory.Services.CreateScope();
        var stateService = scope.ServiceProvider.GetRequiredService<ICacheWarmupStateService>();
        // Ensure cache is not warmed up initially
        stateService.IsWarmedUp.Should().BeFalse("cache should not be warmed up initially");

        // Act - Publish cache warmup completion message
        await MessagePublisher.PublishJobCompletedMessageAsync(
            JobConstants.QueueNames.GuiCacheWarmupCompleted,
            new { CoinCount = 100, IsFirstWarmup = true }
        );

        // Give the message time to be processed
        await Task.Delay(500);

        // Assert - Server state should be updated
        stateService
            .IsWarmedUp.Should()
            .BeTrue("cache state should be marked as warmed up after message processing");
    }
}
