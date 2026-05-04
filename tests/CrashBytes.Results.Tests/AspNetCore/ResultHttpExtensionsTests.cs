using System.Text.Json;
using System.Threading.Tasks;
using CrashBytes.Results.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace CrashBytes.Results.Tests.AspNetCore;

public class ResultHttpExtensionsTests
{
    private static HttpContext NewContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddRouting();
        var ctx = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(int Status, string? Body, string? ContentType)> ExecuteAsync(IResult result)
    {
        var ctx = NewContext();
        await result.ExecuteAsync(ctx);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(ctx.Response.Body);
        var body = await reader.ReadToEndAsync();
        return (ctx.Response.StatusCode, body.Length == 0 ? null : body, ctx.Response.ContentType);
    }

    [Fact]
    public async Task ToHttpResult_NonGenericSuccess_ReturnsNoContent()
    {
        var (status, body, _) = await ExecuteAsync(Result.Success().ToHttpResult());

        Assert.Equal(StatusCodes.Status204NoContent, status);
        Assert.Null(body);
    }

    [Fact]
    public async Task ToHttpResult_NonGenericFailure_ReturnsProblemDetails()
    {
        var (status, body, contentType) = await ExecuteAsync(
            Result.Failure("not_found", "missing").ToHttpResult());

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.NotNull(body);
        Assert.Contains("application/problem+json", contentType ?? "");

        using var doc = JsonDocument.Parse(body!);
        Assert.Equal("not_found", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal("missing", doc.RootElement.GetProperty("detail").GetString());
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ToHttpResult_GenericSuccess_ReturnsOkWithValue()
    {
        var (status, body, _) = await ExecuteAsync(Result.Success(new { id = 7, name = "x" }).ToHttpResult());

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.NotNull(body);
        using var doc = JsonDocument.Parse(body!);
        Assert.Equal(7, doc.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("x", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ToHttpResult_GenericFailure_ReturnsProblemDetails()
    {
        var (status, body, _) = await ExecuteAsync(
            Result.Failure<int>("validation", "bad input").ToHttpResult());

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.NotNull(body);
        using var doc = JsonDocument.Parse(body!);
        Assert.Equal("validation", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ToHttpResult_GenericSuccess_WithCustomProjector_UsesProjector()
    {
        var (status, body, _) = await ExecuteAsync(
            Result.Success("abc").ToHttpResult(id => HttpResults.Created($"/items/{id}", new { id })));

        Assert.Equal(StatusCodes.Status201Created, status);
        Assert.NotNull(body);
    }

    [Fact]
    public async Task ToHttpResult_GenericFailure_WithCustomProjector_StillReturnsProblem()
    {
        var (status, _, _) = await ExecuteAsync(
            Result.Failure<string>("conflict", "duplicate").ToHttpResult(
                id => HttpResults.Created($"/items/{id}", new { id })));

        Assert.Equal(StatusCodes.Status409Conflict, status);
    }

    [Fact]
    public void ToHttpResult_NullResult_Throws()
    {
        Result? result = null;
        Assert.Throws<ArgumentNullException>(() => result!.ToHttpResult());

        Result<int>? gen = null;
        Assert.Throws<ArgumentNullException>(() => gen!.ToHttpResult());
        Assert.Throws<ArgumentNullException>(() => gen!.ToHttpResult(_ => HttpResults.Ok()));
    }

    [Fact]
    public void ToHttpResult_NullProjector_Throws()
    {
        var ok = Result.Success(1);
        Assert.Throws<ArgumentNullException>(() => ok.ToHttpResult(null!));
    }
}
