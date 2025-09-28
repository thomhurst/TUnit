using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Exception assertion with lazy evaluation
/// </summary>
public class ExceptionAssertion<TException> : AssertionBase<TException>
    where TException : Exception
{
    private readonly Type? _expectedType;
    private string? _expectedMessage;
    private Func<TException, bool>? _predicate;

    // Internal property to access the actual value provider for chaining extensions
    internal Func<Task<TException>> ActualValueProvider => GetActualValueAsync;

    // Constructor for async delegates that should throw
    public ExceptionAssertion(Func<Task> asyncDelegate)
        : base(async () => await CaptureExceptionAsync(asyncDelegate))
    {
    }

    // Constructor for sync delegates that should throw
    public ExceptionAssertion(Action syncDelegate)
        : base(() => Task.FromResult(CaptureException(syncDelegate)))
    {
    }

    // Constructor with expected type validation
    public ExceptionAssertion(Func<Task> asyncDelegate, Type expectedType)
        : base(async () => await CaptureExceptionAsync(asyncDelegate))
    {
        _expectedType = expectedType;
    }

    public ExceptionAssertion(Action syncDelegate, Type expectedType)
        : base(() => Task.FromResult(CaptureException(syncDelegate)))
    {
        _expectedType = expectedType;
    }

    // Fluent configuration methods
    public ExceptionAssertion<TException> WithMessage(string expectedMessage)
    {
        _expectedMessage = expectedMessage;
        return this;
    }

    public ExceptionAssertion<TException> WithMessageContaining(string substring)
    {
        _predicate = ex => ex.Message?.Contains(substring) ?? false;
        return this;
    }

    public ExceptionAssertion<TException> WithMessageContaining(string substring, StringComparison comparison)
    {
        _predicate = ex => ex.Message?.Contains(substring, comparison) ?? false;
        return this;
    }

    public ExceptionAssertion<TException> HasMessageContaining(string substring)
    {
        // Store the existing predicate and combine with new one
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && (ex.Message?.Contains(substring) ?? false);
        }
        else
        {
            _predicate = ex => ex.Message?.Contains(substring) ?? false;
        }
        return this;
    }

    // Support And chaining by returning self
    public new ExceptionAssertion<TException> And => this;

    public ExceptionAssertion<TException> WithMessageMatching(string pattern)
    {
        // Store the existing predicate and combine with new one
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && System.Text.RegularExpressions.Regex.IsMatch(ex.Message ?? "", pattern);
        }
        else
        {
            _predicate = ex => System.Text.RegularExpressions.Regex.IsMatch(ex.Message ?? "", pattern);
        }
        return this;
    }

    public ExceptionAssertion<TException> WithMessageMatching(StringMatcher matcher)
    {
        // Store the existing predicate and combine with new one
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && matcher.Matches(ex.Message);
        }
        else
        {
            _predicate = ex => matcher.Matches(ex.Message);
        }
        return this;
    }

    public ExceptionAssertion<TException> WithMessageNotContaining(string substring)
    {
        // Store the existing predicate and combine with new one
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && !(ex.Message?.Contains(substring) ?? false);
        }
        else
        {
            _predicate = ex => !(ex.Message?.Contains(substring) ?? false);
        }
        return this;
    }

    public ExceptionAssertion<TException> WithMessageNotContaining(string substring, StringComparison comparison)
    {
        // Store the existing predicate and combine with new one
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && !(ex.Message?.Contains(substring, comparison) ?? false);
        }
        else
        {
            _predicate = ex => !(ex.Message?.Contains(substring, comparison) ?? false);
        }
        return this;
    }

    public ExceptionAssertion<TException> WithParameterName(string parameterName)
    {
        if (typeof(TException).IsAssignableFrom(typeof(ArgumentException)))
        {
            var existingPredicate = _predicate;
            if (existingPredicate != null)
            {
                _predicate = ex => existingPredicate(ex) && (ex as ArgumentException)?.ParamName == parameterName;
            }
            else
            {
                _predicate = ex => (ex as ArgumentException)?.ParamName == parameterName;
            }
        }
        return this;
    }

    public ExceptionAssertion<TException> Matching(Func<TException, bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    public ExceptionAssertion<TException> HasInnerException()
    {
        _predicate = ex => ex.InnerException != null;
        return this;
    }

    public ExceptionAssertion<TException> HasNoInnerException()
    {
        _predicate = ex => ex.InnerException == null;
        return this;
    }

    public ExceptionAssertion<TException> HasStackTrace()
    {
        _predicate = ex => !string.IsNullOrEmpty(ex.StackTrace);
        return this;
    }

    // IsAssignableTo - checks if the exception is assignable to the target type
    public CustomAssertion<TException> IsAssignableTo<TTarget>()
    {
        return new CustomAssertion<TException>(GetActualValueAsync,
            ex => typeof(TTarget).IsAssignableFrom(ex?.GetType()),
            $"Expected exception to be assignable to {typeof(TTarget).Name}");
    }

    public CustomAssertion<TException> IsAssignableTo(Type targetType)
    {
        return new CustomAssertion<TException>(GetActualValueAsync,
            ex => targetType.IsAssignableFrom(ex?.GetType()),
            $"Expected exception to be assignable to {targetType.Name}");
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var exception = await GetActualValueAsync();

        // Check if we got an exception
        if (exception == null)
        {
            return AssertionResult.Fail("Expected an exception but none was thrown");
        }

        // Check type if specified
        if (_expectedType != null && !_expectedType.IsInstanceOfType(exception))
        {
            return AssertionResult.Fail($"Expected exception of type {_expectedType.Name} but got {exception.GetType().Name}");
        }

        // Check message if specified
        if (_expectedMessage != null && exception.Message != _expectedMessage)
        {
            return AssertionResult.Fail($"Expected exception message '{_expectedMessage}' but got '{exception.Message}'");
        }

        // Check predicate if specified
        if (_predicate != null && !_predicate(exception))
        {
            return AssertionResult.Fail($"Exception did not match the specified condition");
        }

        return AssertionResult.Passed;
    }

    private static async Task<TException> CaptureExceptionAsync(Func<Task> asyncDelegate)
    {
        try
        {
            await asyncDelegate();
            return null!; // Will be caught by assertion logic
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static TException CaptureException(Action syncDelegate)
    {
        try
        {
            syncDelegate();
            return null!; // Will be caught by assertion logic
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    public ExceptionAssertion<TException> WithInnerException()
    {
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && ex.InnerException != null;
        }
        else
        {
            _predicate = ex => ex.InnerException != null;
        }
        return this;
    }

    public ExceptionAssertion<TException> WithInnerException<TInner>()
        where TInner : Exception
    {
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && ex.InnerException is TInner;
        }
        else
        {
            _predicate = ex => ex.InnerException is TInner;
        }
        return this;
    }

    public ExceptionAssertion<TException> HasMessageEqualTo(string expectedMessage)
    {
        var existingPredicate = _predicate;
        if (existingPredicate != null)
        {
            _predicate = ex => existingPredicate(ex) && ex.Message == expectedMessage;
        }
        else
        {
            _predicate = ex => ex.Message == expectedMessage;
        }
        return this;
    }

    /// <summary>
    /// Implicit conversion that executes the assertion and returns the exception
    /// </summary>
    public static implicit operator Task<TException>(ExceptionAssertion<TException> assertion)
    {
        return assertion.GetExceptionAsync();
    }

    /// <summary>
    /// Executes the assertion and returns the exception
    /// </summary>
    public async Task<TException> GetExceptionAsync()
    {
        await ExecuteAsync();
        return await GetActualValueAsync();
    }

    /// <summary>
    /// Custom GetAwaiter that returns the exception when awaited
    /// </summary>
    public new System.Runtime.CompilerServices.TaskAwaiter<TException> GetAwaiter()
    {
        return GetExceptionAsync().GetAwaiter();
    }
}

// Non-generic version for type validation at runtime
public class ExceptionAssertion : ExceptionAssertion<Exception>
{
    public ExceptionAssertion(Func<Task> asyncDelegate) : base(asyncDelegate)
    {
    }

    public ExceptionAssertion(Action syncDelegate) : base(syncDelegate)
    {
    }

    public ExceptionAssertion(Func<Task> asyncDelegate, Type expectedType) : base(asyncDelegate, expectedType)
    {
    }

    public ExceptionAssertion(Action syncDelegate, Type expectedType) : base(syncDelegate, expectedType)
    {
    }

    public new ExceptionAssertion WithInnerException()
    {
        return (ExceptionAssertion)base.WithInnerException();
    }
}