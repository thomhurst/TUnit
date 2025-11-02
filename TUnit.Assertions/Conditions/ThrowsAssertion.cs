using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for exception assertions that provides common type-checking functionality.
/// Uses self-referencing generic pattern to maintain type-safe fluent API.
/// </summary>
public abstract class BaseThrowsAssertion<TException, TSelf> : Assertion<TException>
    where TException : Exception
    where TSelf : BaseThrowsAssertion<TException, TSelf>
{
    private readonly bool _allowSubclasses;

    protected BaseThrowsAssertion(
        AssertionContext<TException> context,
        bool allowSubclasses)
        : base(context)
    {
        _allowSubclasses = allowSubclasses;
    }

    /// <summary>
    /// Checks if the exception type matches according to the specific assertion rules.
    /// </summary>
    protected abstract bool CheckExceptionType(Exception actualException, out string? errorMessage);

    /// <summary>
    /// Gets whether this assertion checks for exact type match.
    /// </summary>
    protected abstract bool IsExactTypeMatch { get; }

    protected sealed override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var exception = metadata.Value;
        var evaluationException = metadata.Exception;

        // If there was an evaluation exception, something went wrong during evaluation
        if (evaluationException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {evaluationException.GetType().FullName}"));
        }

        // The exception should be in the value field after MapException
        if (exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        // Delegate type checking to derived class
        if (!CheckExceptionType(exception, out var typeErrorMessage))
        {
            return Task.FromResult(AssertionResult.Failed(typeErrorMessage!));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        $"to throw {(IsExactTypeMatch ? "exactly " : "")}{typeof(TException).Name}";
}

/// <summary>
/// Asserts that a delegate throws a specific exception type (or subclass).
/// Checks the exception captured during evaluation.
/// </summary>
public class ThrowsAssertion<TException> : BaseThrowsAssertion<TException, ThrowsAssertion<TException>>
    where TException : Exception
{
    public ThrowsAssertion(
        AssertionContext<TException> context)
        : base(context, allowSubclasses: true)
    {
    }

    protected override bool IsExactTypeMatch => false;

    protected override bool CheckExceptionType(Exception actualException, out string? errorMessage)
    {
        if (actualException is not TException)
        {
            errorMessage = $"wrong exception type: {actualException.GetType().Name} instead of {typeof(TException).Name}";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Creates an assertion for the inner exception.
    /// The returned assertion can be used to assert properties of the inner exception.
    /// </summary>
    public ThrowsAssertion<Exception> WithInnerException()
    {
        Context.ExpressionBuilder.Append(".WithInnerException()");

        // Create a new evaluation context that evaluates to the inner exception
        var innerExceptionContext = new EvaluationContext<Exception>(async () =>
        {
            var (value, exception) = await Context.GetAsync();

            // After MapException, the exception is in the value field
            var actualException = value;
            var inner = actualException?.InnerException;

            return (inner, null);  // Inner exception as value
        });

        return new ThrowsAssertion<Exception>(new AssertionContext<Exception>(innerExceptionContext, Context.ExpressionBuilder));
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message contains the specified substring.
    /// </summary>
    public ExceptionMessageContainsAssertion<TException> WithMessageContaining(string expectedSubstring)
    {
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return new ExceptionMessageContainsAssertion<TException>(Context, expectedSubstring);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message contains the specified substring using the specified comparison.
    /// </summary>
    public ExceptionMessageContainsAssertion<TException> WithMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageContainsAssertion<TException>(Context, expectedSubstring, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message does NOT contain the specified substring.
    /// </summary>
    public ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining(string notExpectedSubstring)
    {
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\")");
        return new ExceptionMessageNotContainsAssertion<TException>(Context, notExpectedSubstring);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message does NOT contain the specified substring using the specified comparison.
    /// </summary>
    public ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining(string notExpectedSubstring, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageNotContainsAssertion<TException>(Context, notExpectedSubstring, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message exactly equals the specified string.
    /// </summary>
    public ExceptionMessageEqualsAssertion<TException> WithMessage(string expectedMessage)
    {
        Context.ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\")");
        return new ExceptionMessageEqualsAssertion<TException>(Context, expectedMessage);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message exactly equals the specified string using the specified comparison.
    /// </summary>
    public ExceptionMessageEqualsAssertion<TException> WithMessage(string expectedMessage, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\", StringComparison.{comparison})");
        return new ExceptionMessageEqualsAssertion<TException>(Context, expectedMessage, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message matches a wildcard pattern.
    /// </summary>
    public ExceptionMessageMatchesPatternAssertion<TException> WithMessageMatching(string pattern)
    {
        Context.ExpressionBuilder.Append($".WithMessageMatching(\"{pattern}\")");
        return new ExceptionMessageMatchesPatternAssertion<TException>(Context, pattern);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message matches a StringMatcher pattern.
    /// </summary>
    public ExceptionMessageMatchesAssertion<TException> WithMessageMatching(StringMatcher matcher)
    {
        Context.ExpressionBuilder.Append($".WithMessageMatching(matcher)");
        return new ExceptionMessageMatchesAssertion<TException>(Context, matcher);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that an ArgumentException has the specified parameter name.
    /// </summary>
    public ExceptionParameterNameAssertion<TException> WithParameterName(string expectedParameterName)
    {
        Context.ExpressionBuilder.Append($".WithParameterName(\"{expectedParameterName}\")");
        return new ExceptionParameterNameAssertion<TException>(Context, expectedParameterName);
    }

    /// <summary>
    /// Adds runtime Type-based exception checking for non-generic Throws scenarios.
    /// Returns a specialized assertion that validates against the provided Type.
    /// </summary>
    public async Task<Exception?> WithExceptionType(Type expectedExceptionType)
    {
        if (!typeof(Exception).IsAssignableFrom(expectedExceptionType))
        {
            throw new ArgumentException($"Type {expectedExceptionType.Name} must be an Exception type", nameof(expectedExceptionType));
        }

        // Await the current assertion to get the exception
        await this;
        var (exception, _) = await Context.GetAsync();

        // Now validate it's the correct type
        if (exception != null && !expectedExceptionType.IsInstanceOfType(exception))
        {
            throw new Exceptions.AssertionException($"Expected {expectedExceptionType.Name} but got {exception.GetType().Name}: {exception.Message}");
        }

        return exception;
    }
}

/// <summary>
/// Asserts that a delegate throws exactly the specified exception type (not subclasses).
/// </summary>
public class ThrowsExactlyAssertion<TException> : BaseThrowsAssertion<TException, ThrowsExactlyAssertion<TException>>
    where TException : Exception
{
    public ThrowsExactlyAssertion(
        AssertionContext<TException> context)
        : base(context, allowSubclasses: false)
    {
    }

    protected override bool IsExactTypeMatch => true;

    protected override bool CheckExceptionType(Exception actualException, out string? errorMessage)
    {
        // Exact type match - not subclasses
        if (actualException.GetType() != typeof(TException))
        {
            errorMessage = $"wrong exception type: {actualException.GetType().Name} instead of exactly {typeof(TException).Name}";
            return false;
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message contains the specified substring.
    /// </summary>
    public ExceptionMessageContainsAssertion<TException> WithMessageContaining(string expectedSubstring)
    {
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return new ExceptionMessageContainsAssertion<TException>(Context, expectedSubstring);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message contains the specified substring using the specified comparison.
    /// </summary>
    public ExceptionMessageContainsAssertion<TException> WithMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageContainsAssertion<TException>(Context, expectedSubstring, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message does NOT contain the specified substring.
    /// </summary>
    public ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining(string notExpectedSubstring)
    {
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\")");
        return new ExceptionMessageNotContainsAssertion<TException>(Context, notExpectedSubstring);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message does NOT contain the specified substring using the specified comparison.
    /// </summary>
    public ExceptionMessageNotContainsAssertion<TException> WithMessageNotContaining(string notExpectedSubstring, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\", StringComparison.{comparison})");
        return new ExceptionMessageNotContainsAssertion<TException>(Context, notExpectedSubstring, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message exactly equals the specified string.
    /// </summary>
    public ExceptionMessageEqualsAssertion<TException> WithMessage(string expectedMessage)
    {
        Context.ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\")");
        return new ExceptionMessageEqualsAssertion<TException>(Context, expectedMessage);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message exactly equals the specified string using the specified comparison.
    /// </summary>
    public ExceptionMessageEqualsAssertion<TException> WithMessage(string expectedMessage, StringComparison comparison)
    {
        Context.ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\", StringComparison.{comparison})");
        return new ExceptionMessageEqualsAssertion<TException>(Context, expectedMessage, comparison);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message matches a wildcard pattern.
    /// </summary>
    public ExceptionMessageMatchesPatternAssertion<TException> WithMessageMatching(string pattern)
    {
        Context.ExpressionBuilder.Append($".WithMessageMatching(\"{pattern}\")");
        return new ExceptionMessageMatchesPatternAssertion<TException>(Context, pattern);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that the exception message matches a StringMatcher pattern.
    /// </summary>
    public ExceptionMessageMatchesAssertion<TException> WithMessageMatching(StringMatcher matcher)
    {
        Context.ExpressionBuilder.Append($".WithMessageMatching(matcher)");
        return new ExceptionMessageMatchesAssertion<TException>(Context, matcher);
    }

    /// <summary>
    /// Instance method for backward compatibility - delegates to extension method.
    /// Asserts that an ArgumentException has the specified parameter name.
    /// </summary>
    public ExceptionParameterNameAssertion<TException> WithParameterName(string expectedParameterName)
    {
        Context.ExpressionBuilder.Append($".WithParameterName(\"{expectedParameterName}\")");
        return new ExceptionParameterNameAssertion<TException>(Context, expectedParameterName, requireExactType: true);
    }
}

/// <summary>
/// Asserts that a delegate does not throw any exception.
/// Generic version that preserves the return value type.
/// </summary>
public class ThrowsNothingAssertion<TValue> : Assertion<TValue>
{
    public ThrowsNothingAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"threw {exception.GetType().FullName}: {exception.Message}"));
        }

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
        AssertionContext<TValue> context,
        string expectedMessage,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expectedMessage = expectedMessage;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        Exception? exceptionToCheck = null;

        // If we have an exception parameter (from Throws/ThrowsExactly), use that
        if (exception is Exception ex)
        {
            exceptionToCheck = ex;
        }
        // Otherwise, the value should be an exception (direct assertion on exception)
        else if (value is Exception valueAsException)
        {
            exceptionToCheck = valueAsException;
        }
        else if (value == null && exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }
        else
        {
            return Task.FromResult(AssertionResult.Failed($"value is not an exception (type: {value?.GetType().Name ?? "null"})"));
        }

        if (string.Equals(exceptionToCheck.Message, _expectedMessage, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"message was \"{exceptionToCheck.Message}\""));
    }

    protected override string GetExpectation() => $"to have message equal to \"{_expectedMessage}\"";
}

/// <summary>
/// Asserts that an exception's Message property starts with the expected string.
/// Works with both direct exception assertions and chained exception assertions (via .And).
/// </summary>
public class HasMessageStartingWithAssertion<TValue> : Assertion<TValue>
{
    private readonly string _expectedPrefix;
    private readonly StringComparison _comparison;

    public HasMessageStartingWithAssertion(
        AssertionContext<TValue> context,
        string expectedPrefix,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expectedPrefix = expectedPrefix;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        Exception? exceptionToCheck = null;

        // If we have an exception parameter (from Throws/ThrowsExactly), use that
        if (exception is Exception ex)
        {
            exceptionToCheck = ex;
        }
        // Otherwise, the value should be an exception (direct assertion on exception)
        else if (value is Exception valueAsException)
        {
            exceptionToCheck = valueAsException;
        }
        else if (value == null && exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }
        else
        {
            return Task.FromResult(AssertionResult.Failed($"value is not an exception (type: {value?.GetType().Name ?? "null"})"));
        }

        if (exceptionToCheck.Message.StartsWith(_expectedPrefix, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"message was \"{exceptionToCheck.Message}\""));
    }

    protected override string GetExpectation() => $"to have message starting with \"{_expectedPrefix}\"";
}

/// <summary>
/// Asserts that an exception's Message property contains the expected substring.
/// Works with both direct exception assertions and chained exception assertions (via .And).
/// </summary>
public class HasMessageContainingAssertion<TValue> : Assertion<TValue>
{
    private readonly string _expectedSubstring;
    private readonly StringComparison _comparison;

    public HasMessageContainingAssertion(
        AssertionContext<TValue> context,
        string expectedSubstring,
        StringComparison comparison = StringComparison.Ordinal)
        : base(context)
    {
        _expectedSubstring = expectedSubstring;
        _comparison = comparison;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        Exception? exceptionToCheck = null;

        // If we have an exception parameter (from Throws/ThrowsExactly), use that
        if (exception is Exception ex)
        {
            exceptionToCheck = ex;
        }
        // Otherwise, the value should be an exception (direct assertion on exception)
        else if (value is Exception valueAsException)
        {
            exceptionToCheck = valueAsException;
        }
        else if (value == null && exception == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }
        else
        {
            return Task.FromResult(AssertionResult.Failed($"value is not an exception (type: {value?.GetType().Name ?? "null"})"));
        }

        if (exceptionToCheck.Message.Contains(_expectedSubstring, _comparison))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"message was \"{exceptionToCheck.Message}\""));
    }

    protected override string GetExpectation() => $"to have message containing \"{_expectedSubstring}\"";
}
