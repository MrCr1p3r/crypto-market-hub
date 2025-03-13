using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;

namespace SharedLibrary.Extensions;

/// <summary>
/// Extension methods for FluentResults
/// </summary>
public static class FluentResultsExtensions
{
    /// <summary>
    /// Converts a Result to an appropriate ActionResult
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller
    ) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : CreateErrorResponse(result.Errors, controller);

    /// <summary>
    /// Converts a Result to an appropriate ActionResult
    /// </summary>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.Ok() : CreateErrorResponse(result.Errors, controller);

    private static IActionResult CreateErrorResponse(List<IError> errors, ControllerBase controller)
    {
        var error = errors.FirstOrDefault();
        if (error == null)
            return controller.StatusCode(StatusCodes.Status500InternalServerError);

        var statusCode = error switch
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

        var problemDetails = new ProblemDetails
        {
            Type = statusCode switch
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
                StatusCodes.Status429TooManyRequests =>
                    "https://datatracker.ietf.org/doc/html/rfc9110",
                StatusCodes.Status500InternalServerError =>
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                StatusCodes.Status502BadGateway =>
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.3",
                StatusCodes.Status503ServiceUnavailable =>
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.4",
                StatusCodes.Status504GatewayTimeout =>
                    "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.5",
                _ => "https://datatracker.ietf.org/doc/html/rfc9110",
            },
            Title = error.GetType().FullName,
            Status = statusCode,
            Detail = error.Message,
            Instance = controller.HttpContext?.Request.Path.Value ?? "context-unavailable",
        };

        return error switch
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
}
