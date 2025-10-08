using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Base class for exception assertions that provides common message validation functionality.
/// Uses self-referencing generic pattern to maintain type-safe fluent API.
/// </summary>
public abstract class BaseThrowsAssertion<TException, TSelf> : Assertion<TException>
    where TException : Exception
    where TSelf : BaseThrowsAssertion<TException, TSelf>
{
    private string? _expectedMessageSubstring;
    private string? _expectedExactMessage;
    private string? _expectedParameterName;
    private string? _notExpectedMessageSubstring;
    private string? _expectedMessagePattern;
    private StringMatcher? _expectedMessageMatcher;
    private StringComparison _stringComparison = StringComparison.Ordinal;

    protected BaseThrowsAssertion(
        AssertionContext<object?> context)
        : base(new AssertionContext<TException>(MapToException(context.Evaluation), context.ExpressionBuilder))
    {
    }

    private static EvaluationContext<TException> MapToException(EvaluationContext<object?> context)
    {
        return context.Map<TException>(exc =>
        {
            if (exc == null)
            {
                return default(TException)!;
            }

            if (exc is not TException typedException)
            {
                throw new InvalidCastException(
                    $"Expected exception of type {typeof(TException).Name} but got {exc.GetType().Name}");
            }

            return typedException;
        });
    }

    /// <summary>
    /// Checks if the exception type matches according to the specific assertion rules.
    /// </summary>
    protected abstract bool CheckExceptionType(Exception actualException, out string? errorMessage);

    /// <summary>
    /// Gets whether this assertion checks for exact type match.
    /// </summary>
    protected abstract bool IsExactTypeMatch { get; }

    /// <summary>
    /// Asserts that the exception message exactly equals the specified string.
    /// </summary>
    public TSelf WithMessage(string expectedMessage)
    {
        _expectedExactMessage = expectedMessage;
        Context.ExpressionBuilder.Append($".WithMessage(\"{expectedMessage}\")");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring.
    /// </summary>
    public TSelf WithMessageContaining(string expectedSubstring)
    {
        _expectedMessageSubstring = expectedSubstring;
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\")");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public TSelf WithMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        _expectedMessageSubstring = expectedSubstring;
        _stringComparison = comparison;
        Context.ExpressionBuilder.Append($".WithMessageContaining(\"{expectedSubstring}\", StringComparison.{comparison})");
        return (TSelf)this;
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring.
    /// </summary>
    public TSelf HasMessageContaining(string expectedSubstring)
    {
        return WithMessageContaining(expectedSubstring);
    }

    /// <summary>
    /// Alias for WithMessageContaining - asserts that the exception message contains the specified substring using the specified string comparison.
    /// </summary>
    public TSelf HasMessageContaining(string expectedSubstring, StringComparison comparison)
    {
        return WithMessageContaining(expectedSubstring, comparison);
    }

    /// <summary>
    /// Asserts that the exception message does NOT contain the specified substring.
    /// </summary>
    public TSelf WithMessageNotContaining(string notExpectedSubstring)
    {
        _notExpectedMessageSubstring = notExpectedSubstring;
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\")");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the exception message does NOT contain the specified substring using the specified string comparison.
    /// </summary>
    public TSelf WithMessageNotContaining(string notExpectedSubstring, StringComparison comparison)
    {
        _notExpectedMessageSubstring = notExpectedSubstring;
        _stringComparison = comparison;
        Context.ExpressionBuilder.Append($".WithMessageNotContaining(\"{notExpectedSubstring}\", StringComparison.{comparison})");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the exception message matches the specified pattern (using wildcards * and ?).
    /// * matches any number of characters, ? matches a single character.
    /// </summary>
    public TSelf WithMessageMatching(string pattern)
    {
        _expectedMessagePattern = pattern;
        Context.ExpressionBuilder.Append($".WithMessageMatching(\"{pattern}\")");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the exception message matches the specified StringMatcher pattern.
    /// Supports regex, wildcards, and case-insensitive matching.
    /// </summary>
    public TSelf WithMessageMatching(StringMatcher matcher)
    {
        _expectedMessageMatcher = matcher;
        Context.ExpressionBuilder.Append($".WithMessageMatching(StringMatcher.{(matcher.IsRegex ? "AsRegex" : "AsWildcard")}(\"{matcher.Pattern}\"){(matcher.IgnoreCase ? ".IgnoringCase()" : "")})");
        return (TSelf)this;
    }

    /// <summary>
    /// Asserts that the ArgumentException has the specified parameter name.
    /// Only valid when TException is ArgumentException or a subclass.
    /// </summary>
    public TSelf WithParameterName(string expectedParameterName)
    {
        _expectedParameterName = expectedParameterName;
        Context.ExpressionBuilder.Append($".WithParameterName(\"{expectedParameterName}\")");
        return (TSelf)this;
    }

    protected sealed override Task<AssertionResult> CheckAsync(EvaluationMetadata<TException> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // For Throws assertions, the exception is stored as the value after mapping
        var actualException = exception ?? value as Exception;

        if (actualException == null)
        {
            return Task.FromResult(AssertionResult.Failed("no exception was thrown"));
        }

        // Delegate type checking to derived class
        if (!CheckExceptionType(actualException, out var typeErrorMessage))
        {
            return Task.FromResult(AssertionResult.Failed(typeErrorMessage!));
        }

        // Validate message expectations
        if (_expectedExactMessage != null && actualException.Message != _expectedExactMessage)
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{actualException.Message}\" does not equal \"{_expectedExactMessage}\""));
        }

        if (_expectedMessageSubstring != null && !actualException.Message.Contains(_expectedMessageSubstring, _stringComparison))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{actualException.Message}\" does not contain \"{_expectedMessageSubstring}\""));
        }

        if (_notExpectedMessageSubstring != null && actualException.Message.Contains(_notExpectedMessageSubstring, _stringComparison))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{actualException.Message}\" should not contain \"{_notExpectedMessageSubstring}\""));
        }

        if (_expectedMessagePattern != null && !MatchesPattern(actualException.Message, _expectedMessagePattern))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{actualException.Message}\" does not match pattern \"{_expectedMessagePattern}\""));
        }

        if (_expectedMessageMatcher != null && !_expectedMessageMatcher.IsMatch(actualException.Message))
        {
            return Task.FromResult(AssertionResult.Failed(
                $"exception message \"{actualException.Message}\" does not match {_expectedMessageMatcher}"));
        }

        // Validate parameter name for ArgumentException
        if (_expectedParameterName != null)
        {
            if (actualException is ArgumentException argumentException)
            {
                if (argumentException.ParamName != _expectedParameterName)
                {
                    return Task.FromResult(AssertionResult.Failed(
                        $"ArgumentException parameter name \"{argumentException.ParamName}\" does not equal \"{_expectedParameterName}\""));
                }
            }
            else
            {
                return Task.FromResult(AssertionResult.Failed(
                    $"WithParameterName can only be used with ArgumentException, but exception is {actualException.GetType().Name}"));
            }
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() =>
        _expectedExactMessage != null
            ? $"to throw {(IsExactTypeMatch ? "exactly " : "")}{typeof(TException).Name} with message \"{_expectedExactMessage}\""
            : _expectedMessageSubstring != null
                ? $"to throw {(IsExactTypeMatch ? "exactly " : "")}{typeof(TException).Name} with message containing \"{_expectedMessageSubstring}\""
                : $"to throw {(IsExactTypeMatch ? "exactly " : "")}{typeof(TException).Name}";

    private static bool MatchesPattern(string input, string pattern)
    {
        // Convert wildcard pattern to regex
        // * matches any number of characters
        // ? matches a single character
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern);
    }
}

/// <summary>
/// Asserts that a delegate throws a specific exception type (or subclass).
/// Checks the exception captured during evaluation.
/// </summary>
public class ThrowsAssertion<TException> : BaseThrowsAssertion<TException, ThrowsAssertion<TException>>
    where TException : Exception
{
    public ThrowsAssertion(
        AssertionContext<object?> context)
        : base(context)
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
        var innerExceptionContext = new EvaluationContext<object?>(async () =>
        {
            var (value, exception) = await Context.GetAsync();
            return (value, exception?.InnerException);
        });

        return new ThrowsAssertion<Exception>(new AssertionContext<object?>(innerExceptionContext, Context.ExpressionBuilder));
    }
}

/// <summary>
/// Asserts that a delegate throws exactly the specified exception type (not subclasses).
/// </summary>
public class ThrowsExactlyAssertion<TException> : BaseThrowsAssertion<TException, ThrowsExactlyAssertion<TException>>
    where TException : Exception
{
    public ThrowsExactlyAssertion(
        AssertionContext<object?> context)
        : base(context)
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
                $"threw {exception.GetType().Name}: {exception.Message}"));
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
