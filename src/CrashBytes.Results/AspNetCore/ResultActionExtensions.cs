using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrashBytes.Results.AspNetCore;

/// <summary>
/// MVC controller helpers that translate <see cref="Result"/> / <see cref="Result{T}"/>
/// values into <see cref="IActionResult"/> instances.
/// </summary>
public static class ResultActionExtensions
{
    /// <summary>
    /// Converts a non-generic <see cref="Result"/> into an <see cref="IActionResult"/>.
    /// Success returns <see cref="NoContentResult"/> (204); failure returns an
    /// <see cref="ObjectResult"/> wrapping a <see cref="ProblemDetails"/>.
    /// </summary>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        if (result.IsSuccess)
            return new NoContentResult();

        var problem = result.Errors.ToProblemDetails();
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into an <see cref="IActionResult"/>.
    /// Success returns <see cref="OkObjectResult"/> (200) with the value; failure returns an
    /// <see cref="ObjectResult"/> wrapping a <see cref="ProblemDetails"/>.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        var problem = result.Errors.ToProblemDetails();
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into an <see cref="IActionResult"/> using
    /// a custom success-status mapper (e.g. for <c>201 Created</c> responses).
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));

        if (result.IsSuccess)
            return onSuccess(result.Value);

        var problem = result.Errors.ToProblemDetails();
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
