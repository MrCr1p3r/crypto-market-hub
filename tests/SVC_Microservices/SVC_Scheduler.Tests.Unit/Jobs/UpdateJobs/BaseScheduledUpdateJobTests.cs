using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using SharedLibrary.Extensions.Testing;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;
using SVC_Scheduler.Jobs.UpdateJobs.Base;

namespace SVC_Scheduler.Tests.Unit.Jobs.UpdateJobs;

public class BaseScheduledUpdateJobTests
{
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly FakeLogger<TestScheduledUpdateJob> _logger;
    private readonly TestScheduledUpdateJob _testJob;

    public BaseScheduledUpdateJobTests()
    {
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _logger = new FakeLogger<TestScheduledUpdateJob>();
        _testJob = new TestScheduledUpdateJob(_mockMessagePublisher.Object, _logger);
    }

    #region Common Invoke Tests

    [Fact]
    public async Task Invoke_WhenExecuteJobAsyncSucceeds_ShouldCallHandleSuccessAndPublishMessage()
    {
        // Arrange
        var testResult = new TestJobResult { Id = 1, Value = "Test" };
        _testJob.SetExecuteJobResult(Result.Ok(testResult));

        // Act
        await _testJob.Invoke();

        // Assert
        _testJob.HandleSuccessCalled.Should().BeTrue();
        _testJob.ReceivedResult.Should().BeEquivalentTo(testResult);
        _testJob.HandleErrorCalled.Should().BeFalse();
        _testJob.HandleExceptionCalled.Should().BeFalse();

        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    "test.routing.key",
                    It.Is<JobCompletedMessage>(msg =>
                        msg.JobName == "TestJob" && msg.Success && msg.Data!.Equals(testResult)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Invoke_WhenExecuteJobAsyncFails_ShouldCallHandleErrorAndPublishErrorMessage()
    {
        // Arrange
        var error = Result.Fail("Test error occurred");
        _testJob.SetExecuteJobResult(error);

        // Act
        await _testJob.Invoke();

        // Assert
        _testJob.HandleErrorCalled.Should().BeTrue();
        _testJob.ReceivedErrors.Should().NotBeEmpty();
        _testJob.HandleSuccessCalled.Should().BeFalse();
        _testJob.HandleExceptionCalled.Should().BeFalse();

        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    "test.routing.key",
                    It.Is<JobCompletedMessage>(static msg =>
                        msg.JobName == "TestJob"
                        && !msg.Success
                        && msg.ErrorMessage!.Contains("Test error occurred")
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Invoke_WhenExecuteJobAsyncThrowsException_ShouldCallHandleExceptionAndRethrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _testJob.SetExecuteJobException(exception);

        // Act & Assert
        var thrownException = await _testJob
            .Invoking(job => job.Invoke())
            .Should()
            .ThrowAsync<InvalidOperationException>();

        thrownException.Which.Message.Should().Be("Test exception");

        _testJob.HandleExceptionCalled.Should().BeTrue();
        _testJob.ReceivedException.Should().Be(exception);
        _testJob.HandleSuccessCalled.Should().BeFalse();
        _testJob.HandleErrorCalled.Should().BeFalse();

        _mockMessagePublisher.Verify(
            publisher =>
                publisher.PublishAsync(
                    "test.routing.key",
                    It.Is<JobCompletedMessage>(msg =>
                        msg.JobName == "TestJob"
                        && !msg.Success
                        && msg.ErrorMessage == "Test exception"
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Invoke_WhenSuccessful_ShouldLogCorrectMessages()
    {
        // Arrange
        var testResult = new TestJobResult { Id = 1, Value = "Test" };
        _testJob.SetExecuteJobResult(Result.Ok(testResult));

        // Act
        await _testJob.Invoke();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Information, "Starting TestJob job");
        _logger.VerifyWasCalled(LogLevel.Information, "TestJob completed successfully");
        _logger.VerifyWasCalled(
            LogLevel.Debug,
            "Published success message for TestJob to test.routing.key"
        );
    }

    [Fact]
    public async Task Invoke_WhenFailed_ShouldLogCorrectMessages()
    {
        // Arrange
        var error = Result.Fail("Test error occurred");
        _testJob.SetExecuteJobResult(error);

        // Act
        await _testJob.Invoke();

        // Assert
        _logger.VerifyWasCalled(LogLevel.Information, "Starting TestJob job");
        _logger.VerifyWasCalled(LogLevel.Error, "TestJob failed");
        _logger.VerifyWasCalled(
            LogLevel.Debug,
            "Published error message for TestJob to test.routing.key"
        );
    }

    [Fact]
    public async Task Invoke_WhenException_ShouldLogCorrectMessages()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _testJob.SetExecuteJobException(exception);

        // Act & Assert
        await _testJob
            .Invoking(job => job.Invoke())
            .Should()
            .ThrowAsync<InvalidOperationException>();

        _logger.VerifyWasCalled(LogLevel.Information, "Starting TestJob job");
        _logger.VerifyWasCalled(LogLevel.Error, "TestJob threw exception");
        _logger.VerifyWasCalled(
            LogLevel.Debug,
            "Published exception message for TestJob to test.routing.key"
        );
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestScheduledUpdateJob(IMessagePublisher messagePublisher, ILogger logger)
        : BaseScheduledUpdateJob<TestJobResult>(messagePublisher, logger)
    {
        private Result<TestJobResult>? _executeJobResult;
        private Exception? _executeJobException;

        protected override string JobName => "TestJob";

        protected override string RoutingKey => "test.routing.key";

        public bool HandleSuccessCalled { get; private set; }

        public bool HandleErrorCalled { get; private set; }

        public bool HandleExceptionCalled { get; private set; }

        public TestJobResult? ReceivedResult { get; private set; }

        public List<IError>? ReceivedErrors { get; private set; }

        public Exception? ReceivedException { get; private set; }

        public long ReceivedElapsedTime { get; private set; }

        public void SetExecuteJobResult(Result<TestJobResult> result)
        {
            _executeJobResult = result;
        }

        public void SetExecuteJobException(Exception exception)
        {
            _executeJobException = exception;
        }

        protected override async Task<Result<TestJobResult>> ExecuteJobAsync()
        {
            if (_executeJobException != null)
            {
                throw _executeJobException;
            }

            await Task.CompletedTask;
            return _executeJobResult ?? Result.Fail("No result set");
        }

        protected override async Task HandleSuccessAsync(
            TestJobResult result,
            long elapsedMilliseconds
        )
        {
            HandleSuccessCalled = true;
            ReceivedResult = result;
            ReceivedElapsedTime = elapsedMilliseconds;
            await base.HandleSuccessAsync(result, elapsedMilliseconds);
        }

        protected override async Task HandleErrorAsync(
            List<IError> errors,
            long elapsedMilliseconds
        )
        {
            HandleErrorCalled = true;
            ReceivedErrors = errors;
            ReceivedElapsedTime = elapsedMilliseconds;
            await base.HandleErrorAsync(errors, elapsedMilliseconds);
        }

        protected override async Task HandleExceptionAsync(Exception ex, long elapsedMilliseconds)
        {
            HandleExceptionCalled = true;
            ReceivedException = ex;
            ReceivedElapsedTime = elapsedMilliseconds;
            await base.HandleExceptionAsync(ex, elapsedMilliseconds);
        }
    }

    private sealed class TestJobResult
    {
        public int Id { get; set; }

        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
