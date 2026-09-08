using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Model;

namespace SearchAPI.Api.Filters;

/// <summary>
/// Rejects a <see cref="SearchRequest"/> with no query words with a 400 before it reaches
/// the controller, so the domain only ever runs against a real query.
/// </summary>
public sealed class ValidateSearchRequestFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var hasWords = context.ActionArguments.Values
            .OfType<SearchRequest>()
            .Any(r => r.Query is { Length: > 0 } && r.Query.Any(w => !string.IsNullOrWhiteSpace(w)));

        if (!hasWords)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Query must contain at least one word.",
                Status = StatusCodes.Status400BadRequest,
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
