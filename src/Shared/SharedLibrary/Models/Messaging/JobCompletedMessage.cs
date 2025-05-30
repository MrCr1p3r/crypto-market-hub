namespace SharedLibrary.Models.Messaging;

/// <summary>
/// Message sent when a scheduled job completes execution.
/// </summary>
public record JobCompletedMessage : BaseMessage
{
    /// <summary>
    /// Name of the job that completed.
    /// </summary>
    public required string JobName { get; init; }

    /// <summary>
    /// Type/category of the job.
    /// </summary>
    public required string JobType { get; init; }

    /// <summary>
    /// When the job completed execution.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Whether the job completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Optional data payload from the job execution.
    /// </summary>
    public object? Data { get; init; }
}
