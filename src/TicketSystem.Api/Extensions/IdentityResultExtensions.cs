using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Extensions;

public static class IdentityResultExtensions
{
    /// <summary>
    /// Renders Identity's error list as a ValidationProblemDetails response, grouping the
    /// descriptions under their Identity error code. Shared by the auth and user endpoints.
    /// </summary>
    public static ObjectResult IdentityFailure(this ControllerBase controller, IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(error => string.IsNullOrWhiteSpace(error.Code) ? "account" : error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).Distinct().ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Account validation failed",
            Instance = controller.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        var response = controller.BadRequest(problem);
        response.ContentTypes.Add("application/problem+json");
        return response;
    }
}
