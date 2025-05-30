namespace SharedLibrary.Models.ProblemDetails;

/// <summary>
/// Represents a reason in the problem details.
/// </summary>
public class ReasonDto
{
    /// <summary>
    /// Gets or sets the reason message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metadata associated with this reason.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the nested reasons.
    /// </summary>
    public IEnumerable<ReasonDto>? Reasons { get; set; }

    /// <summary>
    /// Converts this ReasonDto to a dictionary representation.
    /// </summary>
    /// <returns>A dictionary representation of this reason.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object> { ["message"] = Message };

        if (Metadata != null && Metadata.Count > 0)
        {
            dict["metadata"] = Metadata;
        }

        if (Reasons != null && Reasons.Any())
        {
            dict["reasons"] = Reasons.Select(r => r.ToDictionary());
        }

        return dict;
    }
}
