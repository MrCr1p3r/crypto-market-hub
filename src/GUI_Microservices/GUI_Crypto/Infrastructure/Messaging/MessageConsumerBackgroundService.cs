using GUI_Crypto.Services.Messaging;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Infrastructure.Messaging;

/// <summary>
/// Background service that manages message consumption from RabbitMQ queues.
/// </summary>
public class MessageConsumerBackgroundService(
    IMessageConsumer messageConsumer,
    IServiceScopeFactory serviceScopeFactory
) : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer = messageConsumer;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start consuming market data updates
        await _messageConsumer.StartConsumingAsync<JobCompletedMessage>(
            JobConstants.QueueNames.GuiMarketDataUpdated,
            HandleMarketDataMessage,
            stoppingToken
        );

        // Start consuming kline data updates
        await _messageConsumer.StartConsumingAsync<JobCompletedMessage>(
            JobConstants.QueueNames.GuiKlineDataUpdated,
            HandleKlineDataMessage,
            stoppingToken
        );

        // Start consuming cache warmup completion messages
        await _messageConsumer.StartConsumingAsync<JobCompletedMessage>(
            JobConstants.QueueNames.GuiCacheWarmupCompleted,
            HandleCacheWarmupCompletedMessage,
            stoppingToken
        );

        // Keep the service running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _messageConsumer.StopConsumingAsync();

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handles market data update messages.
    /// </summary>
    /// <param name="message">The market data message to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleMarketDataMessage(JobCompletedMessage message)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<MarketDataMessageHandler>();
        await handler.HandleAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Handles kline data update messages.
    /// </summary>
    /// <param name="message">The kline data message to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleKlineDataMessage(JobCompletedMessage message)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<KlineDataMessageHandler>();
        await handler.HandleAsync(message, CancellationToken.None);
    }

    /// <summary>
    /// Handles cache warmup completion messages.
    /// </summary>
    /// <param name="message">The cache warmup completion message to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleCacheWarmupCompletedMessage(JobCompletedMessage message)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CacheWarmupMessageHandler>();
        await handler.HandleAsync();
    }
}
