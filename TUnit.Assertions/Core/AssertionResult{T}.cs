namespace TUnit.Assertions.Core;

/// <summary>
/// Represents the result of an assertion check that also carries a value when passed.
/// Used by <c>[GenerateAssertion]</c> methods that need to return a value to the caller
/// (terminal assertions).
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public readonly struct AssertionResult<T>
{
    public bool IsPassed { get; }
    public string Message { get; }
    public T? Value { get; }

    private AssertionResult(bool isPassed, string message, T? value)
    {
        IsPassed = isPassed;
        Message = message ?? string.Empty;
        Value = value;
    }

    /// <summary>
    /// Creates a passing assertion result that carries a value.
    /// </summary>
    /// <param name="value">The value to carry with the passing result.</param>
    public static AssertionResult<T> Passed(T value) => new(true, string.Empty, value);

    internal static AssertionResult<T> Failed(string message) => new(false, message, default);

    /// <summary>
    /// Allows implicit conversion from a non-generic <see cref="AssertionResult"/> (failure only).
    /// Converting a passed <see cref="AssertionResult"/> without a value will throw.
    /// </summary>
    public static implicit operator AssertionResult<T>(AssertionResult result)
    {
        if (result.IsPassed)
        {
            throw new InvalidOperationException(
                "Cannot convert a passed AssertionResult to AssertionResult<T> without a value. Use AssertionResult<T>.Passed(value) instead.");
        }

        return Failed(result.Message);
    }
}
