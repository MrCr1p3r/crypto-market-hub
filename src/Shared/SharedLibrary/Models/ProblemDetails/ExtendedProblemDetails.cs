using System.Text.Json;

namespace SharedLibrary.Models.ProblemDetails;

/// <summary>
/// Extended ProblemDetails that provides strongly-typed access to metadata and reasons.
/// </summary>
public class ExtendedProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
{
    /// <summary>
    /// Gets or sets the metadata associated with this problem.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the reasons that caused this problem.
    /// </summary>
    public IEnumerable<ReasonDto>? Reasons { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedProblemDetails"/> class.
    /// </summary>
    public ExtendedProblemDetails() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedProblemDetails"/> class from a regular ProblemDetails.
    /// </summary>
    /// <param name="problemDetails">The source ProblemDetails.</param>
    public ExtendedProblemDetails(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)
    {
        Type = problemDetails.Type;
        Title = problemDetails.Title;
        Status = problemDetails.Status;
        Detail = problemDetails.Detail;
        Instance = problemDetails.Instance;

        // Extract metadata and reasons from extensions
        if (problemDetails.Extensions != null)
        {
            ExtractFromExtensions(problemDetails.Extensions);
        }
    }

    private void ExtractFromExtensions(IDictionary<string, object?> extensions)
    {
        // Extract metadata
        if (
            extensions.TryGetValue("metadata", out var metadataObj)
            && metadataObj is Dictionary<string, object> metadata
        )
        {
            Metadata = metadata;
        }
        else if (
            metadataObj is JsonElement metadataElement
            && metadataElement.ValueKind == JsonValueKind.Object
        )
        {
            Metadata = JsonElementToDictionary(metadataElement);
        }

        // Extract reasons
        if (extensions.TryGetValue("reasons", out var reasonsObj))
        {
            Reasons = ExtractReasons(reasonsObj);
        }
    }

    private static List<ReasonDto> ExtractReasons(object? reasonsObj)
    {
        var reasons = new List<ReasonDto>();

        if (reasonsObj is IEnumerable<object> reasonsList)
        {
            foreach (var reasonItem in reasonsList)
            {
                if (TryCreateReasonDto(reasonItem, out var reason))
                {
                    reasons.Add(reason);
                }
            }
        }
        else if (
            reasonsObj is JsonElement reasonsElement
            && reasonsElement.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var reasonElement in reasonsElement.EnumerateArray())
            {
                if (TryCreateReasonDto(reasonElement, out var reason))
                {
                    reasons.Add(reason);
                }
            }
        }

        return reasons;
    }

    private static bool TryCreateReasonDto(object reasonItem, out ReasonDto reason)
    {
        reason = new ReasonDto();

        if (reasonItem is Dictionary<string, object> reasonDict)
        {
            return TryPopulateReasonFromDictionary(reason, reasonDict);
        }
        else if (
            reasonItem is JsonElement reasonElement
            && reasonElement.ValueKind == JsonValueKind.Object
        )
        {
            return TryPopulateReasonFromJsonElement(reason, reasonElement);
        }

        return false;
    }

    private static bool TryPopulateReasonFromDictionary(
        ReasonDto reason,
        Dictionary<string, object> reasonDict
    )
    {
        if (
            !reasonDict.TryGetValue("message", out var messageObj)
            || messageObj is not string message
        )
        {
            return false;
        }

        reason.Message = message;

        // Extract metadata
        if (
            reasonDict.TryGetValue("metadata", out var metadataObj)
            && metadataObj is Dictionary<string, object> metadata
        )
        {
            reason.Metadata = metadata;
        }

        // Extract nested reasons
        if (reasonDict.TryGetValue("reasons", out var nestedReasonsObj))
        {
            reason.Reasons = ExtractReasons(nestedReasonsObj);
        }

        return true;
    }

    private static bool TryPopulateReasonFromJsonElement(
        ReasonDto reason,
        JsonElement reasonElement
    )
    {
        if (
            !reasonElement.TryGetProperty("message", out var messageElement)
            || messageElement.ValueKind != JsonValueKind.String
        )
        {
            return false;
        }

        reason.Message = messageElement.GetString()!;

        // Extract metadata
        if (
            reasonElement.TryGetProperty("metadata", out var metadataElement)
            && metadataElement.ValueKind == JsonValueKind.Object
        )
        {
            reason.Metadata = JsonElementToDictionary(metadataElement);
        }

        // Extract nested reasons
        if (reasonElement.TryGetProperty("reasons", out var nestedReasonsElement))
        {
            reason.Reasons = ExtractReasons(nestedReasonsElement);
        }

        return true;
    }

    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = JsonElementToObject(property.Value);
        }

        return dictionary;
    }

    private static object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt32(out var intValue)
                ? intValue
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
            JsonValueKind.Undefined => element.ToString(),
            _ => element.ToString(),
        };
    }
}
