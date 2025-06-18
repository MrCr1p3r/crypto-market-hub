using GUI_Crypto.Hubs;
using GUI_Crypto.Infrastructure.Caching;
using GUI_Crypto.Services.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Extensions.Testing;

namespace GUI_Crypto.Tests.Unit.Services.Messaging;

public class CacheWarmupMessageHandlerTests
{
    private readonly Mock<IHubContext<CryptoHub, ICryptoHubClient>> _mockHubContext;
    private readonly Mock<ICacheWarmupStateService> _mockCacheWarmupStateService;
    private readonly FakeLogger<CacheWarmupMessageHandler> _logger;
    private readonly Mock<IHubCallerClients<ICryptoHubClient>> _mockClients;
    private readonly Mock<ICryptoHubClient> _mockCryptoHubClient;
    private readonly CacheWarmupMessageHandler _handler;

    public CacheWarmupMessageHandlerTests()
    {
        _mockHubContext = new Mock<IHubContext<CryptoHub, ICryptoHubClient>>();
        _mockCacheWarmupStateService = new Mock<ICacheWarmupStateService>();
        _logger = new FakeLogger<CacheWarmupMessageHandler>();
        _mockClients = new Mock<IHubCallerClients<ICryptoHubClient>>();
        _mockCryptoHubClient = new Mock<ICryptoHubClient>();

        // Setup hub context chain
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.All).Returns(_mockCryptoHubClient.Object);

        _handler = new CacheWarmupMessageHandler(
            _mockHubContext.Object,
            _mockCacheWarmupStateService.Object,
            _logger
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkCacheAsWarmedUp()
    {
        // Act
        await _handler.HandleAsync();

        // Assert
        _mockCacheWarmupStateService.Verify(service => service.MarkAsWarmedUp(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogCacheWarmupCompletion()
    {
        // Act
        await _handler.HandleAsync();

        // Assert
        _logger.VerifyWasCalled(
            LogLevel.Information,
            "Cache warmup completed successfully - notifying all connected clients"
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldNotifyAllClients()
    {
        // Act
        await _handler.HandleAsync();

        // Assert
        _mockCryptoHubClient.Verify(client => client.ReceiveCacheWarmupCompleted(), Times.Once);
    }
}
