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
    private string? _expectedParameterName;
    private StringComparison _stringComparison = StringComparison.Ordinal;

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
    /// Asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public ThrowsAssertion<TException> WithMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        _expectedMessageSubstring = expectedSubstring;
        _stringComparison = comparison;
        ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return this;
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsAssertion<TException> HasMessageContaining(string expectedSubstring)
    {
        return WithMessageContaining(expectedSubstring);
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public ThrowsAssertion<TException> HasMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        return WithMessageContaining(expectedSubstring, comparison);
    }

    /// <summary>
    /// Asserts that the ArgumentException has the specified parameter name.
    /// Only valid when TException is ArgumentException or a subclass.
    /// </summary>
    public ThrowsAssertion<TException> WithParameterName(string expectedParameterName)
    {
        _expectedParameterName = expectedParameterName;
        ExpressionBuilder.Append($".WithParameterName(\"{expectedParameterName}\")");
        return this;
    }

    /// <summary>
    /// Creates an assertion for the inner exception.
    /// The returned assertion can be used to assert properties of the inner exception.
    /// </summary>
    public ThrowsAssertion<Exception> WithInnerException()
    {
        ExpressionBuilder.Append(".WithInnerException()");

        // Create a new evaluation context that evaluates to the inner exception
        var innerExceptionContext = new EvaluationContext<object?>(async () =>
        {
            var (value, exception) = await Context.GetAsync();
            return (value, exception?.InnerException);
        });

        return new ThrowsAssertion<Exception>(innerExceptionContext, ExpressionBuilder);
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

        if (_expectedMessageSubstring != null && !exception.Message.Contains(_expectedMessageSubstring, _stringComparison))
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not contain \"{_expectedMessageSubstring}\""));

        if (_expectedParameterName != null)
        {
            if (exception is ArgumentException argumentException)
            {
                if (argumentException.ParamName != _expectedParameterName)
                    return Task.FromResult(AssertionResult.Failed(
                        $"ArgumentException parameter name \"{argumentException.ParamName}\" does not equal \"{_expectedParameterName}\""));
            }
            else
            {
                return Task.FromResult(AssertionResult.Failed(
                    $"WithParameterName can only be used with ArgumentException, but exception is {exception.GetType().Name}"));
            }
        }

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
    private string? _expectedParameterName;
    private StringComparison _stringComparison = StringComparison.Ordinal;

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
    /// Asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public ThrowsExactlyAssertion<TException> WithMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        _expectedMessageSubstring = expectedSubstring;
        _stringComparison = comparison;
        ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return this;
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// </summary>
    public ThrowsExactlyAssertion<TException> HasMessageContaining(string expectedSubstring)
    {
        return WithMessageContaining(expectedSubstring);
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public ThrowsExactlyAssertion<TException> HasMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        return WithMessageContaining(expectedSubstring, comparison);
    }

    /// <summary>
    /// Asserts that the ArgumentException has the specified parameter name.
    /// Only valid when TException is ArgumentException or a subclass.
    /// </summary>
    public ThrowsExactlyAssertion<TException> WithParameterName(string expectedParameterName)
    {
        _expectedParameterName = expectedParameterName;
        ExpressionBuilder.Append($".WithParameterName(\"{expectedParameterName}\")");
        return this;
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

        if (_expectedMessageSubstring != null && !exception.Message.Contains(_expectedMessageSubstring, _stringComparison))
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{exception.Message}\" does not contain \"{_expectedMessageSubstring}\""));

        if (_expectedParameterName != null)
        {
            if (exception is ArgumentException argumentException)
            {
                if (argumentException.ParamName != _expectedParameterName)
                    return Task.FromResult(AssertionResult.Failed(
                        $"ArgumentException parameter name \"{argumentException.ParamName}\" does not equal \"{_expectedParameterName}\""));
            }
            else
            {
                return Task.FromResult(AssertionResult.Failed(
                    $"WithParameterName can only be used with ArgumentException, but exception is {exception.GetType().Name}"));
            }
        }

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

/// <summary>
/// Asserts that an exception's Message property equals the expected string.
/// Works with both direct exception assertions and chained exception assertions (via .And).
/// </summary>
public class HasMessageEqualToAssertion<TValue> : Assertion<TValue>
{
    private readonly string _expectedMessage;
    private readonly StringComparison _comparison;

    public HasMessageEqualToAssertion(
        EvaluationContext<TValue> context,
        string expectedMessage,
        StringBuilder expressionBuilder,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context, expressionBuilder)
    {
        _expectedMessage = expectedMessage;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        Exception? exceptionToCheck = null;

        // If we have an exception parameter (from Throws/ThrowsExactly), use that
        if (exception is Exception ex)
            exceptionToCheck = ex;
        // Otherwise, the value should be an exception (direct assertion on exception)
        else if (value is Exception valueAsException)
            exceptionToCheck = valueAsException;
        else if (value == null && exception == null)
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        else
            return Task.FromResult(AssertionResult.Failed($"value is not an exception (type: {value?.GetType().Name ?? "null"})"));

        if (string.Equals(exceptionToCheck.Message, _expectedMessage, _comparison))
            return Task.FromResult(AssertionResult.Passed);

        return Task.FromResult(AssertionResult.Failed($"message was \"{exceptionToCheck.Message}\""));
    }

    protected override string GetExpectation() => $"to have message equal to \"{_expectedMessage}\"";
}
