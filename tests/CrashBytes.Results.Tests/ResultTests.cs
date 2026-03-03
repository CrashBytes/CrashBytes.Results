namespace CrashBytes.Results.Tests;

public class ErrorTests
{
    [Fact]
    public void Error_StoresCodeAndMessage()
    {
        var error = new Error("NOT_FOUND", "Item not found");
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal("Item not found", error.Message);
    }

    [Fact]
    public void Error_ToString_FormatsCorrectly()
    {
        var error = new Error("ERR", "Something failed");
        Assert.Equal("ERR: Something failed", error.ToString());
    }

    [Fact]
    public void Error_Equality_SameValues_AreEqual()
    {
        var a = new Error("ERR", "msg");
        var b = new Error("ERR", "msg");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Error_Equality_DifferentValues_NotEqual()
    {
        var a = new Error("ERR1", "msg");
        var b = new Error("ERR2", "msg");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Error_NullCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Error(null!, "msg"));
    }

    [Fact]
    public void Error_NullMessage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Error("ERR", null!));
    }
}

public class ResultNonGenericTests
{
    [Fact]
    public void Success_IsSuccess()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_IsFailure()
    {
        var result = Result.Failure("ERR", "Something failed");
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("ERR", result.Errors[0].Code);
    }

    [Fact]
    public void Failure_WithError_IsFailure()
    {
        var error = new Error("ERR", "fail");
        var result = Result.Failure(error);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void Failure_WithMultipleErrors()
    {
        var errors = new[] { new Error("E1", "m1"), new Error("E2", "m2") };
        var result = Result.Failure(errors);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Failure_EmptyErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void Failure_NullErrors_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure((IEnumerable<Error>)null!));
    }

    [Fact]
    public void OnSuccess_Success_ExecutesAction()
    {
        bool called = false;
        Result.Success().OnSuccess(() => called = true);
        Assert.True(called);
    }

    [Fact]
    public void OnSuccess_Failure_DoesNotExecuteAction()
    {
        bool called = false;
        Result.Failure("ERR", "fail").OnSuccess(() => called = true);
        Assert.False(called);
    }

    [Fact]
    public void OnFailure_Failure_ExecutesAction()
    {
        IReadOnlyList<Error>? captured = null;
        Result.Failure("ERR", "fail").OnFailure(errors => captured = errors);
        Assert.NotNull(captured);
        Assert.Single(captured!);
    }

    [Fact]
    public void OnFailure_Success_DoesNotExecuteAction()
    {
        bool called = false;
        Result.Success().OnFailure(_ => called = true);
        Assert.False(called);
    }

    [Fact]
    public void Match_Success_ReturnsSuccessValue()
    {
        var result = Result.Success().Match(() => "ok", _ => "fail");
        Assert.Equal("ok", result);
    }

    [Fact]
    public void Match_Failure_ReturnsFailureValue()
    {
        var result = Result.Failure("ERR", "fail").Match(() => "ok", errors => errors[0].Code);
        Assert.Equal("ERR", result);
    }

    [Fact]
    public void Failure_NullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure((Error)null!));
    }

    [Fact]
    public void OnSuccess_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().OnSuccess(null!));
    }

    [Fact]
    public void OnFailure_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure("ERR", "fail").OnFailure(null!));
    }

    [Fact]
    public void Match_NullOnSuccess_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().Match<string>(null!, _ => "fail"));
    }

    [Fact]
    public void Match_NullOnFailure_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success().Match(() => "ok", (Func<IReadOnlyList<Error>, string>)null!));
    }
}

public class ResultGenericTests
{
    [Fact]
    public void Success_HasValue()
    {
        var result = Result.Success(42);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_AccessingValue_Throws()
    {
        var result = Result.Failure<int>("ERR", "fail");
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_Success_TransformsValue()
    {
        var result = Result.Success(5).Map(x => x * 2);
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Map_Failure_PropagatesFailure()
    {
        var result = Result.Failure<int>("ERR", "fail").Map(x => x * 2);
        Assert.True(result.IsFailure);
        Assert.Equal("ERR", result.Errors[0].Code);
    }

    [Fact]
    public void Bind_Success_ChainsToNextResult()
    {
        var result = Result.Success(5).Bind(x => Result<int>.Success(x + 10));
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void Bind_Success_CanReturnFailure()
    {
        var result = Result.Success(5).Bind<int>(x => Result<int>.Failure("ERR", "too small"));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Bind_Failure_PropagatesWithoutCallingBinder()
    {
        bool called = false;
        var result = Result.Failure<int>("ERR", "fail").Bind(x => { called = true; return Result<int>.Success(x); });
        Assert.False(called);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void OnSuccess_Success_ExecutesWithValue()
    {
        int captured = 0;
        Result.Success(42).OnSuccess(v => captured = v);
        Assert.Equal(42, captured);
    }

    [Fact]
    public void OnSuccess_Failure_DoesNotExecute()
    {
        bool called = false;
        Result.Failure<int>("ERR", "fail").OnSuccess(_ => called = true);
        Assert.False(called);
    }

    [Fact]
    public void OnFailure_Failure_ExecutesWithErrors()
    {
        IReadOnlyList<Error>? captured = null;
        Result.Failure<int>("ERR", "fail").OnFailure(errors => captured = errors);
        Assert.NotNull(captured);
    }

    [Fact]
    public void Match_Success_ReturnsSuccessResult()
    {
        var result = Result.Success(42).Match(v => $"Value: {v}", _ => "fail");
        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void Match_Failure_ReturnsFailureResult()
    {
        var result = Result.Failure<int>("ERR", "fail").Match(v => "ok", errors => errors[0].Message);
        Assert.Equal("fail", result);
    }

    [Fact]
    public void GetValueOrDefault_Success_ReturnsValue()
    {
        Assert.Equal(42, Result.Success(42).GetValueOrDefault(-1));
    }

    [Fact]
    public void GetValueOrDefault_Failure_ReturnsFallback()
    {
        Assert.Equal(-1, Result.Failure<int>("ERR", "fail").GetValueOrDefault(-1));
    }

    [Fact]
    public void ImplicitConversion_Value_CreatesSuccess()
    {
        Result<int> result = 42;
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitConversion_Error_CreatesFailure()
    {
        Result<int> result = new Error("ERR", "fail");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Failure_WithMultipleErrors()
    {
        var errors = new[] { new Error("E1", "m1"), new Error("E2", "m2") };
        var result = Result<int>.Failure(errors);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Failure_NullError_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure((Error)null!));
    }

    [Fact]
    public void Failure_EmptyErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure(Array.Empty<Error>()));
    }

    [Fact]
    public void Failure_NullErrors_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure((IEnumerable<Error>)null!));
    }

    [Fact]
    public void Map_NullSelector_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(1).Map<int>(null!));
    }

    [Fact]
    public void Bind_NullBinder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(1).Bind<int>(null!));
    }

    [Fact]
    public void OnSuccess_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(1).OnSuccess(null!));
    }

    [Fact]
    public void OnFailure_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>("ERR", "fail").OnFailure(null!));
    }

    [Fact]
    public void Match_NullOnSuccess_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(1).Match<string>(null!, _ => "fail"));
    }

    [Fact]
    public void Match_NullOnFailure_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success(1).Match(v => "ok", (Func<IReadOnlyList<Error>, string>)null!));
    }

    [Fact]
    public void OnFailure_OnSuccess_DoesNotExecute()
    {
        bool called = false;
        Result.Success(42).OnFailure(_ => called = true);
        Assert.False(called);
    }

    [Fact]
    public void OnSuccess_OnFailure_DoesNotExecute()
    {
        bool called = false;
        Result.Failure<int>("ERR", "fail").OnSuccess(_ => called = true);
        Assert.False(called);
    }
}

public class ResultFactoryHelperTests
{
    [Fact]
    public void Result_SuccessT_CreatesGenericSuccess()
    {
        var result = Result.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Result_FailureT_CreatesGenericFailure()
    {
        var result = Result.Failure<string>("ERR", "fail");
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Result_FailureT_WithError_CreatesGenericFailure()
    {
        var error = new Error("ERR", "fail");
        var result = Result.Failure<string>(error);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Errors[0]);
    }
}
