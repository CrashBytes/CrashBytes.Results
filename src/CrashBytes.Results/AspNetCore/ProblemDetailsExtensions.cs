using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrashBytes.Results.AspNetCore;

/// <summary>
/// Extension methods that map <see cref="Error"/> values onto RFC 7807 <see cref="ProblemDetails"/>.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Maps a single <see cref="Error"/> onto a <see cref="ProblemDetails"/> instance.
    /// </summary>
    /// <param name="error">The domain error to translate.</param>
    /// <param name="statusCode">
    /// Optional explicit status code. When <c>null</c> the value is resolved via
    /// <see cref="ResultStatusCodeMap.Resolve(Error)"/>.
    /// </param>
    /// <returns>A populated <see cref="ProblemDetails"/> ready to be serialised.</returns>
    public static ProblemDetails ToProblemDetails(this Error error, int? statusCode = null)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));

        var status = statusCode ?? ResultStatusCodeMap.Resolve(error);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = error.Message,
            Type = $"https://crashbytes.com/errors/{error.Code}",
        };
        problem.Extensions["code"] = error.Code;
        return problem;
    }

    /// <summary>
    /// Maps a collection of <see cref="Error"/> values onto a single <see cref="ProblemDetails"/>.
    /// All error codes/messages are surfaced through the <c>errors</c> extension property so
    /// callers do not lose information when multiple errors exist.
    /// </summary>
    public static ProblemDetails ToProblemDetails(this IReadOnlyList<Error> errors, int? statusCode = null)
    {
        if (errors is null) throw new ArgumentNullException(nameof(errors));
        if (errors.Count == 0)
            throw new ArgumentException("At least one error is required.", nameof(errors));

        var status = statusCode ?? ResultStatusCodeMap.Resolve(errors);
        var primary = errors[0];
        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = primary.Message,
            Type = $"https://crashbytes.com/errors/{primary.Code}",
        };
        problem.Extensions["code"] = primary.Code;
        problem.Extensions["errors"] = errors
            .Select(e => new { code = e.Code, message = e.Message })
            .ToArray();
        return problem;
    }

    internal static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status408RequestTimeout => "Request Timeout",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
        _ when status >= 500 => "Server Error",
        _ when status >= 400 => "Client Error",
        _ => "Error",
    };
}
