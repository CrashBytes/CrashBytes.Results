using Microsoft.AspNetCore.Http;

namespace CrashBytes.Results.AspNetCore;

/// <summary>
/// Maps domain <see cref="Error"/> codes to HTTP status codes for ASP.NET Core integration.
/// </summary>
/// <remarks>
/// The default mapping recognises a small set of common, framework-agnostic error codes
/// (case-insensitive). Consumers can register their own mappings via <see cref="Register"/>
/// or override the resolution entirely with <see cref="Resolver"/>.
/// </remarks>
public static class ResultStatusCodeMap
{
    private static readonly Dictionary<string, int> _defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["validation"] = StatusCodes.Status400BadRequest,
        ["bad_request"] = StatusCodes.Status400BadRequest,
        ["invalid"] = StatusCodes.Status400BadRequest,
        ["unauthorized"] = StatusCodes.Status401Unauthorized,
        ["forbidden"] = StatusCodes.Status403Forbidden,
        ["not_found"] = StatusCodes.Status404NotFound,
        ["conflict"] = StatusCodes.Status409Conflict,
        ["unprocessable"] = StatusCodes.Status422UnprocessableEntity,
        ["too_many_requests"] = StatusCodes.Status429TooManyRequests,
        ["timeout"] = StatusCodes.Status408RequestTimeout,
        ["unavailable"] = StatusCodes.Status503ServiceUnavailable,
    };

    private static readonly Dictionary<string, int> _custom = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object _gate = new();

    /// <summary>
    /// Optional override delegate. When set, this is consulted before the registered/default
    /// table. Returning <c>null</c> falls back to the default lookup.
    /// </summary>
    public static Func<Error, int?>? Resolver { get; set; }

    /// <summary>
    /// Registers a custom error-code-to-status mapping. Codes are matched case-insensitively.
    /// Custom registrations take precedence over the built-in defaults.
    /// </summary>
    public static void Register(string code, int statusCode)
    {
        if (code is null) throw new ArgumentNullException(nameof(code));
        lock (_gate) _custom[code] = statusCode;
    }

    /// <summary>
    /// Removes any custom mappings registered via <see cref="Register"/>. Defaults are preserved.
    /// </summary>
    public static void ClearCustom()
    {
        lock (_gate) _custom.Clear();
    }

    /// <summary>
    /// Resolves the HTTP status code for the supplied error.
    /// Falls back to <see cref="StatusCodes.Status400BadRequest"/> when no mapping matches.
    /// </summary>
    public static int Resolve(Error error)
    {
        if (error is null) throw new ArgumentNullException(nameof(error));

        if (Resolver is { } resolver)
        {
            var overridden = resolver(error);
            if (overridden.HasValue) return overridden.Value;
        }

        lock (_gate)
        {
            if (_custom.TryGetValue(error.Code, out var custom)) return custom;
        }

        return _defaults.TryGetValue(error.Code, out var status)
            ? status
            : StatusCodes.Status400BadRequest;
    }

    /// <summary>
    /// Resolves the highest-priority status code for a collection of errors.
    /// Server errors (5xx) win over client errors (4xx); within a class the highest code wins.
    /// </summary>
    public static int Resolve(IReadOnlyList<Error> errors)
    {
        if (errors is null) throw new ArgumentNullException(nameof(errors));
        if (errors.Count == 0) return StatusCodes.Status400BadRequest;

        int best = 0;
        foreach (var error in errors)
        {
            var code = Resolve(error);
            // Prefer 5xx over 4xx; otherwise prefer the larger code.
            if (best == 0 ||
                (code >= 500 && best < 500) ||
                (code / 100 == best / 100 && code > best))
            {
                best = code;
            }
        }
        return best == 0 ? StatusCodes.Status400BadRequest : best;
    }
}
