namespace CrashBytes.Results;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed class Error
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public override string ToString() => $"{Code}: {Message}";

    public override bool Equals(object? obj) =>
        obj is Error other && Code == other.Code && Message == other.Message;

    public override int GetHashCode() => HashCode.Combine(Code, Message);
}

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, Array.Empty<Error>());

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result Failure(string code, string message) =>
        new(false, new[] { new Error(code, message) });

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result Failure(Error error) =>
        new(false, new[] { error ?? throw new ArgumentNullException(nameof(error)) });

    /// <summary>Creates a failed result with multiple errors.</summary>
    public static Result Failure(IEnumerable<Error> errors)
    {
        if (errors is null) throw new ArgumentNullException(nameof(errors));
        var list = errors.ToArray();
        if (list.Length == 0) throw new ArgumentException("At least one error is required.", nameof(errors));
        return new Result(false, list);
    }

    /// <summary>Creates a successful result with a value.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(code, message);

    /// <summary>Creates a failed result with a single error.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>
    /// Executes <paramref name="action"/> if the result is a success.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (IsSuccess) action();
        return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> if the result is a failure.
    /// </summary>
    public Result OnFailure(Action<IReadOnlyList<Error>> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (IsFailure) action(Errors);
        return this;
    }

    /// <summary>
    /// Returns one of two values depending on success or failure.
    /// </summary>
    public T Match<T>(Func<T> onSuccess, Func<IReadOnlyList<Error>, T> onFailure)
    {
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));
        return IsSuccess ? onSuccess() : onFailure(Errors);
    }
}

/// <summary>
/// Represents the result of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value. Throws <see cref="InvalidOperationException"/> if the result is a failure.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>Creates a successful result with a value.</summary>
    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());

    /// <summary>Creates a failed result with a single error.</summary>
    public new static Result<T> Failure(string code, string message) =>
        new(false, default, new[] { new Error(code, message) });

    /// <summary>Creates a failed result with a single error.</summary>
    public new static Result<T> Failure(Error error) =>
        new(false, default, new[] { error ?? throw new ArgumentNullException(nameof(error)) });

    /// <summary>Creates a failed result with multiple errors.</summary>
    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        if (errors is null) throw new ArgumentNullException(nameof(errors));
        var list = errors.ToArray();
        if (list.Length == 0) throw new ArgumentException("At least one error is required.", nameof(errors));
        return new Result<T>(false, default, list);
    }

    /// <summary>
    /// Transforms the value if successful, propagating failure otherwise.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        return IsSuccess
            ? Result<TOut>.Success(selector(Value))
            : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Chains to another Result-producing operation if successful, propagating failure otherwise.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        if (binder is null) throw new ArgumentNullException(nameof(binder));
        return IsSuccess ? binder(Value) : Result<TOut>.Failure(Errors);
    }

    /// <summary>
    /// Executes <paramref name="action"/> with the value if the result is a success.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (IsSuccess) action(Value);
        return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> if the result is a failure.
    /// </summary>
    public new Result<T> OnFailure(Action<IReadOnlyList<Error>> action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        if (IsFailure) action(Errors);
        return this;
    }

    /// <summary>
    /// Returns one of two values depending on success or failure.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        if (onSuccess is null) throw new ArgumentNullException(nameof(onSuccess));
        if (onFailure is null) throw new ArgumentNullException(nameof(onFailure));
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    /// <summary>
    /// Returns the value if successful, or <paramref name="fallback"/> otherwise.
    /// </summary>
    public T GetValueOrDefault(T fallback = default!) =>
        IsSuccess ? Value : fallback;

    /// <summary>Implicit conversion from a value to a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicit conversion from an Error to a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    private static new Result<T> Failure(IReadOnlyList<Error> errors) =>
        new(false, default, errors);
}
