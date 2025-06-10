using System.Text.Json;
using GUI_Crypto.Services.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Extensions.Testing;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Tests.Unit.Services.Messaging;

public class BaseMessageHandlerTests
{
    private readonly FakeLogger<TestMessageHandler> _logger;
    private readonly TestMessageHandler _testHandler;

    public BaseMessageHandlerTests()
    {
        _logger = new FakeLogger<TestMessageHandler>();
        _testHandler = new TestMessageHandler(_logger);
    }

    #region Common HandleAsync Tests

    [Fact]
    public async Task HandleAsync_WhenJobFailed_ShouldLogWarningAndNotCallHandleSuccess()
    {
        // Arrange
        var failedMessage = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = false,
            ErrorMessage = "Test error occurred",
            Data = null,
            Source = "TestSource",
        };
        var cancellationToken = CancellationToken.None;

        // Act
        await _testHandler.HandleAsync(failedMessage, cancellationToken);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Warning, "Job TestJob failed");
        _testHandler.HandleSuccessCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithoutData_ShouldLogDebugAndNotCallHandleSuccess()
    {
        // Arrange
        var successMessageWithoutData = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = null,
            Source = "TestSource",
        };
        var cancellationToken = CancellationToken.None;

        // Act
        await _testHandler.HandleAsync(successMessageWithoutData, cancellationToken);

        // Assert
        _logger.VerifyWasCalled(LogLevel.Debug, "Job TestJob completed successfully without data");
        _testHandler.HandleSuccessCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenDataTypeIsInvalid_ShouldThrowJsonException()
    {
        // Arrange
        var invalidDataMessage = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = 12345, // Invalid data type (not JsonElement or string)
            Source = "TestSource",
        };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await _testHandler
            .Invoking(h => h.HandleAsync(invalidDataMessage, cancellationToken))
            .Should()
            .ThrowAsync<JsonException>();

        exception.Which.Message.Should().Contain("Invalid data type: Int32");
        _testHandler.HandleSuccessCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenJsonDeserializationFails_ShouldThrowJsonException()
    {
        // Arrange
        var invalidJsonMessage = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = "{ invalid json }", // Invalid JSON format
            Source = "TestSource",
        };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await _testHandler
            .Invoking(h => h.HandleAsync(invalidJsonMessage, cancellationToken))
            .Should()
            .ThrowAsync<JsonException>();

        _testHandler.HandleSuccessCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessWithValidData_ShouldCallHandleSuccess()
    {
        // Arrange
        var testData = new TestMessageData
        {
            Id = 1,
            Name = "Test",
            Value = 42.5m,
        };
        var jsonData = JsonSerializer.Serialize(testData);
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonData);

        var successMessage = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = jsonElement,
            Source = "TestSource",
        };
        var cancellationToken = CancellationToken.None;

        // Act
        await _testHandler.HandleAsync(successMessage, cancellationToken);

        // Assert
        _testHandler.HandleSuccessCalled.Should().BeTrue();
        _testHandler.ReceivedMessage.Should().Be(successMessage);
        _testHandler.ReceivedData.Should().BeEquivalentTo(testData);
        _testHandler.ReceivedCancellationToken.Should().Be(cancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationTokenProvided_ShouldPassItToHandleSuccess()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var message = new JobCompletedMessage
        {
            JobName = "TestJob",
            JobType = "TestType",
            CompletedAt = DateTime.UtcNow,
            Success = true,
            ErrorMessage = null,
            Data = null,
            Source = "TestSource",
        };

        // Act & Assert
        await _testHandler
            .Invoking(h => h.HandleAsync(message, cts.Token))
            .Should()
            .NotThrowAsync();
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestMessageHandler(ILogger<TestMessageHandler> logger)
        : BaseMessageHandler<TestMessageData>(logger)
    {
        public bool HandleSuccessCalled { get; private set; }

        public TestMessageData? ReceivedData { get; private set; }

        public JobCompletedMessage? ReceivedMessage { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        protected override async Task HandleSuccess(
            JobCompletedMessage message,
            TestMessageData data,
            CancellationToken cancellationToken
        )
        {
            HandleSuccessCalled = true;
            ReceivedData = data;
            ReceivedMessage = message;
            ReceivedCancellationToken = cancellationToken;
            await Task.CompletedTask;
        }
    }

    private sealed class TestMessageData
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }

    #endregion
}
