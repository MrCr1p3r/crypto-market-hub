using System.Diagnostics;
using Coravel.Invocable;
using FluentResults;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;

namespace SVC_Scheduler.Jobs.Base;

/// <summary>
/// Abstract base class for scheduled jobs that provides common functionality like
/// logging, error handling, message publishing, and performance tracking.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the job execution.</typeparam>
public abstract class BaseScheduledUpdateJob<TResult>(
    IMessagePublisher messagePublisher,
    ILogger logger
) : IInvocable
{
    private const string JobType = JobConstants.Types.DataSync;
    private const string Source = JobConstants.Sources.Scheduler;
    private readonly IMessagePublisher _messagePublisher = messagePublisher;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Gets the name of the job for logging and messaging.
    /// </summary>
    protected abstract string JobName { get; }

    /// <summary>
    /// Gets the routing key for message publishing.
    /// </summary>
    protected abstract string RoutingKey { get; }

    /// <summary>
    /// Coravel entry point - orchestrates the job execution with cross-cutting concerns.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Invoke()
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        _logger.LogJobStarting(JobName);
        _logger.LogJobExecutionStarted(JobName, JobType, startTime);

        try
        {
            var result = await ExecuteJobAsync();

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                await HandleSuccessAsync(result.Value, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await HandleErrorAsync(result.Errors, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await HandleExceptionAsync(ex, stopwatch.ElapsedMilliseconds);
            throw; // Re-throw for Coravel to handle
        }
    }

    /// <summary>
    /// Executes the core business logic of the job.
    /// </summary>
    /// <returns>A Result containing the job execution outcome.</returns>
    protected abstract Task<Result<TResult>> ExecuteJobAsync();

    /// <summary>
    /// Handles successful job completion.
    /// </summary>
    /// <param name="result">The successful result from job execution.</param>
    /// <param name="elapsedMilliseconds">Time taken for job execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task HandleSuccessAsync(TResult result, long elapsedMilliseconds)
    {
        _logger.LogJobCompletedSuccessfully(JobName, elapsedMilliseconds);

        var message = CreateSuccessMessage(result);
        await _messagePublisher.PublishAsync(RoutingKey, message);

        _logger.LogSuccessMessagePublished(JobName, RoutingKey);
        _logger.LogJobResultProcessed(JobName, true, !Equals(result, default(TResult)));
    }

    /// <summary>
    /// Handles job errors (non-exception failures).
    /// </summary>
    /// <param name="errors">List of errors that occurred during job execution.</param>
    /// <param name="elapsedMilliseconds">Time taken for job execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task HandleErrorAsync(List<IError> errors, long elapsedMilliseconds)
    {
        var errorMessage = string.Join("; ", errors.Select(error => error.Message));

        _logger.LogJobFailed(JobName, elapsedMilliseconds, errorMessage);

        var message = CreateErrorMessage(errorMessage);
        await _messagePublisher.PublishAsync(RoutingKey, message);

        _logger.LogErrorMessagePublished(JobName, RoutingKey);
        _logger.LogJobResultProcessed(JobName, false, false);
    }

    /// <summary>
    /// Handles job exceptions.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="elapsedMilliseconds">Time taken for job execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual async Task HandleExceptionAsync(Exception ex, long elapsedMilliseconds)
    {
        _logger.LogJobException(ex, JobName, elapsedMilliseconds);

        var message = CreateExceptionMessage(ex);
        await _messagePublisher.PublishAsync(RoutingKey, message);

        _logger.LogExceptionMessagePublished(JobName, RoutingKey);
        _logger.LogJobResultProcessed(JobName, false, false);
    }

    /// <summary>
    /// Creates a success message with the job result.
    /// </summary>
    /// <param name="result">The successful job result.</param>
    /// <returns>A JobCompletedMessage representing the successful job completion.</returns>
    private JobCompletedMessage CreateSuccessMessage(TResult result)
    {
        return new JobCompletedMessage
        {
            JobName = JobName,
            JobType = JobType,
            CompletedAt = DateTime.UtcNow,
            Success = true,
            Data = result,
            Source = Source,
        };
    }

    /// <summary>
    /// Creates an error message when the job fails.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A JobCompletedMessage representing the failed job completion.</returns>
    private JobCompletedMessage CreateErrorMessage(string errorMessage)
    {
        return new JobCompletedMessage
        {
            JobName = JobName,
            JobType = JobType,
            CompletedAt = DateTime.UtcNow,
            Success = false,
            ErrorMessage = errorMessage,
            Source = Source,
        };
    }

    /// <summary>
    /// Creates an exception message when the job throws an exception.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <returns>A JobCompletedMessage representing the failed job completion due to exception.</returns>
    private JobCompletedMessage CreateExceptionMessage(Exception ex)
    {
        return new JobCompletedMessage
        {
            JobName = JobName,
            JobType = JobType,
            CompletedAt = DateTime.UtcNow,
            Success = false,
            ErrorMessage = ex.Message,
            Source = Source,
        };
    }
}
