namespace SVC_Coins.Infrastructure.Configuration;

/// <summary>
/// Represents the settings required for database configuration.
/// </summary>
public class DatabaseSettings
{
    public const string SectionName = "Database";

    public required string Server { get; init; }

    public required string Name { get; init; }

    public required string User { get; init; }

    public required string Password { get; init; }
}
