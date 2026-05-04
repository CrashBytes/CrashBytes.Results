using CrashBytes.Results.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrashBytes.Results.Tests.AspNetCore;

public class ResultActionExtensionsTests
{
    [Fact]
    public void ToActionResult_NonGenericSuccess_ReturnsNoContent()
    {
        var actionResult = Result.Success().ToActionResult();
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_NonGenericFailure_ReturnsObjectResultWithProblemDetails()
    {
        var actionResult = Result.Failure("not_found", "missing").ToActionResult();

        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("missing", problem.Detail);
        Assert.Equal("not_found", problem.Extensions["code"]);
    }

    [Fact]
    public void ToActionResult_GenericSuccess_ReturnsOkObjectResultWithValue()
    {
        var actionResult = Result.Success(42).ToActionResult();

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public void ToActionResult_GenericFailure_ReturnsObjectResultWithProblemDetails()
    {
        var actionResult = Result.Failure<int>("validation", "name required").ToActionResult();

        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("validation", problem.Extensions["code"]);
    }

    [Fact]
    public void ToActionResult_GenericSuccess_WithCustomProjection_UsesProjector()
    {
        var actionResult = Result.Success("created-id").ToActionResult(
            id => new CreatedResult($"/items/{id}", id));

        var created = Assert.IsType<CreatedResult>(actionResult);
        Assert.Equal("/items/created-id", created.Location);
        Assert.Equal("created-id", created.Value);
    }

    [Fact]
    public void ToActionResult_GenericFailure_WithCustomProjection_StillReturnsProblem()
    {
        var actionResult = Result.Failure<string>("conflict", "duplicate").ToActionResult(
            v => new CreatedResult("/items/x", v));

        var obj = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, obj.StatusCode);
    }

    [Fact]
    public void ToActionResult_NullResult_Throws()
    {
        Result? result = null;
        Assert.Throws<ArgumentNullException>(() => result!.ToActionResult());

        Result<int>? gen = null;
        Assert.Throws<ArgumentNullException>(() => gen!.ToActionResult());
        Assert.Throws<ArgumentNullException>(() => gen!.ToActionResult(_ => new OkResult()));
    }

    [Fact]
    public void ToActionResult_NullProjector_Throws()
    {
        var ok = Result.Success(1);
        Assert.Throws<ArgumentNullException>(() => ok.ToActionResult(null!));
    }
}
