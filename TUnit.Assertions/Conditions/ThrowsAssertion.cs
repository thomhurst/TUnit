using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a delegate throws a specific exception type (or subclass).
/// Checks the exception captured during evaluation.
/// </summary>
public class ThrowsAssertion<TException> : Assertion<object?>
    where TException : Exception
{
    private string? _expectedMessageSubstring;
    private string? _expectedExactMessage;

    public ThrowsAssertion(
        EvaluationContext<object?> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    /// <summary>
    /// Asserts that the exception message exactly equals the specified string.
    /// </summary>
    public ThrowsAssertion<TException> WithMessage(string expectedMessage)
    {
        _expectedExactMessage = expectedMessage;
        ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\")");
        return this;
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsAssertion<TException> WithMessageContaining(string expectedSubstring)
    {
        _expectedMessageSubstring = expectedSubstring;
        ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return this;
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsAssertion<TException> HasMessageContaining(string expectedSubstring)
    {
        return WithMessageContaining(expectedSubstring);
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        if (exception == null)
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));

        if (exception is not TException)
            return Task.FromResult(AssertionResult.Failed(
                $"wrong exception type: {exception.GetType().Name} instead of {typeof(TException).Name}"));

        if (_expectedExactMessage != null && exception.Message != _expectedExactMessage)
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not equal \"{_expectedExactMessage}\""));

        if (_expectedMessageSubstring != null && !exception.Message.Contains(_expectedMessageSubstring))
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not contain \"{_expectedMessageSubstring}\""));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        _expectedExactMessage != null
            ? $"to throw {typeof(TException).Name} with message \"{_expectedExactMessage}\""
            : _expectedMessageSubstring != null
                ? $"to throw {typeof(TException).Name} with message containing \"{_expectedMessageSubstring}\""
                : $"to throw {typeof(TException).Name}";
}

/// <summary>
/// Asserts that a delegate throws exactly the specified exception type (not subclasses).
/// </summary>
public class ThrowsExactlyAssertion<TException> : Assertion<object?>
    where TException : Exception
{
    private string? _expectedMessageSubstring;
    private string? _expectedExactMessage;

    public ThrowsExactlyAssertion(
        EvaluationContext<object?> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    /// <summary>
    /// Asserts that the exception message exactly equals the specified string.
    /// </summary>
    public ThrowsExactlyAssertion<TException> WithMessage(string expectedMessage)
    {
        _expectedExactMessage = expectedMessage;
        ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\")");
        return this;
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsExactlyAssertion<TException> WithMessageContaining(string expectedSubstring)
    {
        _expectedMessageSubstring = expectedSubstring;
        ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return this;
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsExactlyAssertion<TException> HasMessageContaining(string expectedSubstring)
    {
        return WithMessageContaining(expectedSubstring);
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        if (exception == null)
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));

        // Exact type match - not subclasses
        if (exception.GetType() != typeof(TException))
            return Task.FromResult(AssertionResult.Failed(
                $"wrong exception type: {exception.GetType().Name} instead of exactly {typeof(TException).Name}"));

        if (_expectedExactMessage != null && exception.Message != _expectedExactMessage)
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not equal \"{_expectedExactMessage}\""));

        if (_expectedMessageSubstring != null && !exception.Message.Contains(_expectedMessageSubstring))
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not contain \"{_expectedMessageSubstring}\""));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        _expectedExactMessage != null
            ? $"to throw exactly {typeof(TException).Name} with message \"{_expectedExactMessage}\""
            : _expectedMessageSubstring != null
                ? $"to throw exactly {typeof(TException).Name} with message containing \"{_expectedMessageSubstring}\""
                : $"to throw exactly {typeof(TException).Name}";
}

/// <summary>
/// Asserts that a delegate does not throw any exception.
/// </summary>
public class ThrowsNothingAssertion : Assertion<object?>
{
    public ThrowsNothingAssertion(
        EvaluationContext<object?> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed(
                $"threw {exception.GetType().Name}: {exception.Message}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not throw any exception";
}
