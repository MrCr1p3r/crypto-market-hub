using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;

namespace SharedLibrary.Extensions;

/// <summary>
/// Extension methods for FluentResults.
/// </summary>
public static class FluentResultsExtensions
{
    /// <summary>
    /// Converts a Result to an appropriate ActionResult.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller instance.</param>
    /// <returns>An appropriate <see cref="IActionResult"/> based on the result.</returns>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller
    ) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : CreateErrorResponse(result.Errors[0], controller);

    /// <summary>
    /// Converts a Result to an appropriate ActionResult.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller instance.</param>
    /// <returns>An appropriate <see cref="IActionResult"/> based on the result.</returns>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : CreateErrorResponse(result.Errors[0], controller);

    private static ObjectResult CreateErrorResponse(IError error, ControllerBase controller)
    {
        var statusCode = GetStatusCode(error);
        var problemDetailsType = GetProblemDetailsType(statusCode);
        var extensions = BuildProblemDetailsExtensions(error);

        var problemDetails = new ProblemDetails
        {
            Type = problemDetailsType,
            Title = error.GetType().Name,
            Status = statusCode,
            Detail = error.Message,
            Instance = controller.HttpContext?.Request.Path.Value ?? "context-unavailable",
            Extensions = extensions.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value),
        };

        return ToCorrectObjectResult(controller, error, problemDetails);
    }

    private static int GetStatusCode(IError error) =>
        error switch
        {
            GenericErrors.BadRequestError => StatusCodes.Status400BadRequest,
            GenericErrors.UnauthorizedError => StatusCodes.Status401Unauthorized,
            GenericErrors.ForbiddenError => StatusCodes.Status403Forbidden,
            GenericErrors.NotFoundError => StatusCodes.Status404NotFound,
            GenericErrors.ConflictError => StatusCodes.Status409Conflict,
            GenericErrors.TooManyRequestsError => StatusCodes.Status429TooManyRequests,
            GenericErrors.InternalError => StatusCodes.Status500InternalServerError,
            GenericErrors.GatewayError => StatusCodes.Status502BadGateway,
            GenericErrors.UnavailableError => StatusCodes.Status503ServiceUnavailable,
            GenericErrors.TimeoutError => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError,
        };

    private static string GetProblemDetailsType(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
            StatusCodes.Status401Unauthorized =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
            StatusCodes.Status403Forbidden =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4",
            StatusCodes.Status404NotFound =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5",
            StatusCodes.Status409Conflict =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10",
            StatusCodes.Status429TooManyRequests => "https://datatracker.ietf.org/doc/html/rfc9110",
            StatusCodes.Status500InternalServerError =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            StatusCodes.Status502BadGateway =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.3",
            StatusCodes.Status503ServiceUnavailable =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4",
            StatusCodes.Status504GatewayTimeout =>
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.5",
            _ => "https://datatracker.ietf.org/doc/html/rfc9110",
        };

    private static Dictionary<string, object> BuildProblemDetailsExtensions(IError error)
    {
        var extensions = new Dictionary<string, object>();

        extensions.AddMetadataAndReasonsFrom(error);

        return extensions;
    }

    private static void AddMetadataAndReasonsFrom(
        this Dictionary<string, object> extensions,
        IError error
    )
    {
        if (error.Metadata.Count != 0)
        {
            extensions["metadata"] = new Dictionary<string, object?>(error.Metadata);
        }

        if (error.Reasons.Count != 0)
        {
            extensions["reasons"] = error.Reasons.Select(BuildReasonObject);
        }
    }

    private static Dictionary<string, object> BuildReasonObject(IError reason)
    {
        var reasonDto = new Dictionary<string, object>() { ["message"] = reason.Message };

        reasonDto.AddMetadataAndReasonsFrom(reason);

        return reasonDto;
    }

    private static ObjectResult ToCorrectObjectResult(
        ControllerBase controller,
        IError error,
        ProblemDetails problemDetails
    ) =>
        error switch
        {
            GenericErrors.BadRequestError => controller.BadRequest(problemDetails),
            GenericErrors.NotFoundError => controller.NotFound(problemDetails),
            GenericErrors.UnauthorizedError => controller.Unauthorized(problemDetails),
            GenericErrors.ForbiddenError => controller.StatusCode(
                StatusCodes.Status403Forbidden,
                problemDetails
            ),
            GenericErrors.ConflictError => controller.Conflict(problemDetails),
            GenericErrors.TooManyRequestsError => controller.StatusCode(
                StatusCodes.Status429TooManyRequests,
                problemDetails
            ),
            GenericErrors.InternalError => controller.StatusCode(
                StatusCodes.Status500InternalServerError,
                problemDetails
            ),
            GenericErrors.GatewayError => controller.StatusCode(
                StatusCodes.Status502BadGateway,
                problemDetails
            ),
            GenericErrors.UnavailableError => controller.StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                problemDetails
            ),
            GenericErrors.TimeoutError => controller.StatusCode(
                StatusCodes.Status504GatewayTimeout,
                problemDetails
            ),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, problemDetails),
        };
}
