using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CrashBytes.Results.AspNetCore;

/// <summary>
/// Minimal-API helpers that translate <see cref="Result"/> / <see cref="Result{T}"/>
/// values into <see cref="IResult"/> instances usable from endpoint route handlers.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Converts a non-generic <see cref="Result"/> into an <see cref="IResult"/>.
    /// Success returns <c>Results.NoContent()</c> (204); failure returns
    /// <c>Results.Problem(ProblemDetails)</c>.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        if (result.IsSuccess)
            return HttpResults.NoContent();

        var problem = result.Errors.ToProblemDetails();
        return HttpResults.Problem(problem);
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into an <see cref="IResult"/>.
    /// Success returns <c>Results.Ok(value)</c> with the value; failure returns
    /// <c>Results.Problem(ProblemDetails)</c>.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));

        if (result.IsSuccess)
            return HttpResults.Ok(result.Value);

        var problem = result.Errors.ToProblemDetails();
        return HttpResults.Problem(problem);
    }

    /// <summary>
    /// Converts a <see cref="Result{T}"/> into an <see cref="IResult"/> using a custom
    /// success projector (e.g. <c>Results.Created(uri, value)</c>).
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));

        if (result.IsSuccess)
            return onSuccess(result.Value);

        var problem = result.Errors.ToProblemDetails();
        return HttpResults.Problem(problem);
    }
}
