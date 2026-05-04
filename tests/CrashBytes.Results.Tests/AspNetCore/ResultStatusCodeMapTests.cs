using CrashBytes.Results.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace CrashBytes.Results.Tests.AspNetCore;

[Collection(nameof(ResultStatusCodeMapCollection))]
public class ResultStatusCodeMapTests : IDisposable
{
    public ResultStatusCodeMapTests()
    {
        // Defensive reset: Resolver / custom registrations are static.
        ResultStatusCodeMap.Resolver = null;
        ResultStatusCodeMap.ClearCustom();
    }

    public void Dispose()
    {
        ResultStatusCodeMap.Resolver = null;
        ResultStatusCodeMap.ClearCustom();
    }

    [Theory]
    [InlineData("validation", StatusCodes.Status400BadRequest)]
    [InlineData("bad_request", StatusCodes.Status400BadRequest)]
    [InlineData("invalid", StatusCodes.Status400BadRequest)]
    [InlineData("unauthorized", StatusCodes.Status401Unauthorized)]
    [InlineData("forbidden", StatusCodes.Status403Forbidden)]
    [InlineData("not_found", StatusCodes.Status404NotFound)]
    [InlineData("conflict", StatusCodes.Status409Conflict)]
    [InlineData("unprocessable", StatusCodes.Status422UnprocessableEntity)]
    [InlineData("too_many_requests", StatusCodes.Status429TooManyRequests)]
    [InlineData("timeout", StatusCodes.Status408RequestTimeout)]
    [InlineData("unavailable", StatusCodes.Status503ServiceUnavailable)]
    public void Resolve_KnownCode_ReturnsExpectedStatus(string code, int expected)
    {
        var actual = ResultStatusCodeMap.Resolve(new Error(code, "msg"));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        Assert.Equal(StatusCodes.Status404NotFound, ResultStatusCodeMap.Resolve(new Error("NOT_FOUND", "msg")));
    }

    [Fact]
    public void Resolve_UnknownCode_DefaultsTo400()
    {
        Assert.Equal(StatusCodes.Status400BadRequest, ResultStatusCodeMap.Resolve(new Error("nope", "msg")));
    }

    [Fact]
    public void Register_OverridesDefault()
    {
        ResultStatusCodeMap.Register("not_found", StatusCodes.Status410Gone);

        Assert.Equal(StatusCodes.Status410Gone, ResultStatusCodeMap.Resolve(new Error("not_found", "msg")));
    }

    [Fact]
    public void Resolver_TakesPrecedenceOverDefaults()
    {
        ResultStatusCodeMap.Resolver = err => err.Code == "custom" ? StatusCodes.Status418ImATeapot : (int?)null;

        Assert.Equal(StatusCodes.Status418ImATeapot, ResultStatusCodeMap.Resolve(new Error("custom", "msg")));
        // When resolver returns null, falls back to defaults.
        Assert.Equal(StatusCodes.Status404NotFound, ResultStatusCodeMap.Resolve(new Error("not_found", "msg")));
    }

    [Fact]
    public void Resolve_List_PrefersServerErrorOverClientError()
    {
        IReadOnlyList<Error> errors = new[]
        {
            new Error("not_found", "x"),       // 404
            new Error("unavailable", "x"),     // 503
            new Error("validation", "x"),      // 400
        };

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ResultStatusCodeMap.Resolve(errors));
    }

    [Fact]
    public void Resolve_List_WithinSameClass_PicksHighest()
    {
        IReadOnlyList<Error> errors = new[]
        {
            new Error("validation", "x"),  // 400
            new Error("not_found", "x"),   // 404
            new Error("conflict", "x"),    // 409
        };

        Assert.Equal(StatusCodes.Status409Conflict, ResultStatusCodeMap.Resolve(errors));
    }

    [Fact]
    public void Resolve_NullError_Throws()
    {
        Error? error = null;
        Assert.Throws<ArgumentNullException>(() => ResultStatusCodeMap.Resolve(error!));
    }

    [Fact]
    public void Resolve_NullErrorList_Throws()
    {
        IReadOnlyList<Error>? errors = null;
        Assert.Throws<ArgumentNullException>(() => ResultStatusCodeMap.Resolve(errors!));
    }

    [Fact]
    public void Register_NullCode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ResultStatusCodeMap.Register(null!, 400));
    }
}

[CollectionDefinition(nameof(ResultStatusCodeMapCollection), DisableParallelization = true)]
public class ResultStatusCodeMapCollection { }
