using CrashBytes.Results.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace CrashBytes.Results.Tests.AspNetCore;

public class ProblemDetailsExtensionsTests
{
    [Fact]
    public void ToProblemDetails_PopulatesShape()
    {
        var error = new Error("not_found", "User 42 not found");

        var problem = error.ToProblemDetails();

        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal("User 42 not found", problem.Detail);
        Assert.Equal("https://crashbytes.com/errors/not_found", problem.Type);
        Assert.Equal("not_found", problem.Extensions["code"]);
    }

    [Fact]
    public void ToProblemDetails_ExplicitStatusCode_OverridesMapping()
    {
        var error = new Error("not_found", "missing");

        var problem = error.ToProblemDetails(statusCode: StatusCodes.Status410Gone);

        Assert.Equal(StatusCodes.Status410Gone, problem.Status);
    }

    [Fact]
    public void ToProblemDetails_NullError_Throws()
    {
        Error? error = null;
        Assert.Throws<ArgumentNullException>(() => error!.ToProblemDetails());
    }

    [Fact]
    public void ToProblemDetails_OnErrorList_SurfacesAllErrors()
    {
        IReadOnlyList<Error> errors = new[]
        {
            new Error("validation", "name is required"),
            new Error("validation", "email is invalid"),
        };

        var problem = errors.ToProblemDetails();

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("validation", problem.Extensions["code"]);
        Assert.NotNull(problem.Extensions["errors"]);
        // The "errors" extension is an array of {code, message} pairs.
        var array = (Array)problem.Extensions["errors"]!;
        Assert.Equal(2, array.Length);
    }

    [Fact]
    public void ToProblemDetails_OnEmptyList_Throws()
    {
        IReadOnlyList<Error> empty = Array.Empty<Error>();
        Assert.Throws<ArgumentException>(() => empty.ToProblemDetails());
    }

    [Fact]
    public void ToProblemDetails_OnNullList_Throws()
    {
        IReadOnlyList<Error>? errors = null;
        Assert.Throws<ArgumentNullException>(() => errors!.ToProblemDetails());
    }

    [Fact]
    public void ToProblemDetails_UnknownCode_DefaultsTo400()
    {
        var error = new Error("totally_made_up_code", "boom");

        var problem = error.ToProblemDetails();

        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
    }
}
