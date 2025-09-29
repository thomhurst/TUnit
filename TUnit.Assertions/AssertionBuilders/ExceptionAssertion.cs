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
    private readonly string _assertionMethod;
    private readonly string _callerExpression;
    private readonly Func<Task>? _asyncDelegate;
    private readonly Action? _syncDelegate;

    // Internal property to access the actual value provider for chaining extensions
    internal Func<Task<TException>> ActualValueProvider => GetActualValueAsync;

    // Constructor for async delegates that should throw
    public ExceptionAssertion(Func<Task> asyncDelegate, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(() => Task.FromResult(default(TException)!))
    {
        _asyncDelegate = asyncDelegate;
        _assertionMethod = assertionMethod;
        _callerExpression = callerExpression;
    }

    // Constructor for sync delegates that should throw
    public ExceptionAssertion(Action syncDelegate, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(() => Task.FromResult(default(TException)!))
    {
        _syncDelegate = syncDelegate;
        _assertionMethod = assertionMethod;
        _callerExpression = callerExpression;
    }

    // Constructor with expected type validation
    public ExceptionAssertion(Func<Task> asyncDelegate, Type expectedType, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(() => Task.FromResult(default(TException)!))
    {
        _asyncDelegate = asyncDelegate;
        _expectedType = expectedType;
        _assertionMethod = assertionMethod;
        _callerExpression = callerExpression;
    }

    public ExceptionAssertion(Action syncDelegate, Type expectedType, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(() => Task.FromResult(default(TException)!))
    {
        _syncDelegate = syncDelegate;
        _expectedType = expectedType;
        _assertionMethod = assertionMethod;
        _callerExpression = callerExpression;
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
        // Convert string pattern to StringMatcher (defaults to wildcard)
        StringMatcher matcher = pattern; // Uses implicit operator to convert to wildcard
        return WithMessageMatching(matcher);
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
        TException? exception = null;
        Exception? wrongException = null;

        // Capture the exception by executing the delegate
        try
        {
            if (_asyncDelegate != null)
            {
                await _asyncDelegate();
            }
            else if (_syncDelegate != null)
            {
                _syncDelegate();
            }
            // No exception was thrown
        }
        catch (TException ex)
        {
            exception = ex;
        }
        catch (Exception ex)
        {
            wrongException = ex;
        }

        // Check if we got an exception
        if (exception == null && wrongException == null)
        {
            // Format the message based on the assertion method
            string message;
            if (_assertionMethod == "ThrowsExactly" && typeof(TException) != typeof(Exception))
            {
                var article = GetArticleFor(typeof(TException).Name);
                message = $"Expected action to throw exactly {article} {typeof(TException).Name}\n\nbut none was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{typeof(TException).Name}>()";
            }
            else if (_assertionMethod == "Throws" && typeof(TException) != typeof(Exception))
            {
                var article = GetArticleFor(typeof(TException).Name);
                message = $"Expected action to throw {article} {typeof(TException).Name}\n\nbut none was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{typeof(TException).Name}>()";
            }
            else if (_assertionMethod == "ThrowsException")
            {
                message = $"Expected action to throw an exception\n\nbut none was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}()";
            }
            else
            {
                // Fallback for other cases
                message = "Expected an exception but none was thrown";
            }
            return AssertionResult.Fail(message);
        }

        // Check if wrong exception type was thrown
        if (wrongException != null)
        {
            string message;
            if (_assertionMethod == "ThrowsExactly")
            {
                var expectedArticle = GetArticleFor(typeof(TException).Name);
                var actualArticle = GetArticleFor(wrongException.GetType().Name);
                message = $"Expected action to throw exactly {expectedArticle} {typeof(TException).Name}\n\nbut {actualArticle} {wrongException.GetType().Name} was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{typeof(TException).Name}>()";
            }
            else if (_assertionMethod == "Throws")
            {
                var expectedArticle = GetArticleFor(typeof(TException).Name);
                var actualArticle = GetArticleFor(wrongException.GetType().Name);
                message = $"Expected action to throw {expectedArticle} {typeof(TException).Name}\n\nbut {actualArticle} {wrongException.GetType().Name} was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{typeof(TException).Name}>()";
            }
            else
            {
                message = $"Expected exception of type {typeof(TException).Name} but got {wrongException.GetType().Name}";
            }
            return AssertionResult.Fail(message);
        }

        // Check type if specified (when we have _expectedType but got different type)
        if (_expectedType != null && !_expectedType.IsInstanceOfType(exception))
        {
            string message;
            if (_assertionMethod == "ThrowsExactly")
            {
                var expectedArticle = GetArticleFor(_expectedType.Name);
                var actualArticle = GetArticleFor(exception!.GetType().Name);
                message = $"Expected action to throw exactly {expectedArticle} {_expectedType.Name}\n\nbut {actualArticle} {exception.GetType().Name} was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{_expectedType.Name}>()";
            }
            else if (_assertionMethod == "Throws")
            {
                var expectedArticle = GetArticleFor(_expectedType.Name);
                var actualArticle = GetArticleFor(exception!.GetType().Name);
                message = $"Expected action to throw {expectedArticle} {_expectedType.Name}\n\nbut {actualArticle} {exception.GetType().Name} was thrown\n\nat Assert.That({_callerExpression}).{_assertionMethod}<{_expectedType.Name}>()";
            }
            else
            {
                message = $"Expected exception of type {_expectedType.Name} but got {exception!.GetType().Name}";
            }
            return AssertionResult.Fail(message);
        }

        // Check message if specified
        if (_expectedMessage != null && exception!.Message != _expectedMessage)
        {
            return AssertionResult.Fail($"Expected exception message '{_expectedMessage}' but got '{exception.Message}'");
        }

        // Check predicate if specified
        if (_predicate != null && !_predicate(exception!))
        {
            return AssertionResult.Fail($"Exception message '{exception!.Message}' did not match the specified condition");
        }

        return AssertionResult.Passed;
    }

    /// <summary>
    /// Override message building to provide proper exception assertion formatting
    /// </summary>
    protected override string BuildSingleAssertionErrorMessage(AssertionResult result, TException actualValue)
    {
        // For exception assertions, don't do custom formatting - let the base class handle it
        // or just return the original message as exception assertions have their own specific formats
        return result.Message;
    }

    /// <summary>
    /// Gets the correct article ("a" or "an") for an exception type name
    /// </summary>
    private static string GetArticleFor(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return "a";

        // Check if the first letter is a vowel
        var firstChar = char.ToLowerInvariant(typeName[0]);
        return (firstChar == 'a' || firstChar == 'e' || firstChar == 'i' || firstChar == 'o' || firstChar == 'u')
            ? "an" : "a";
    }

    private async Task<TException> CaptureExceptionForReturnAsync()
    {
        try
        {
            if (_asyncDelegate != null)
            {
                await _asyncDelegate();
            }
            else if (_syncDelegate != null)
            {
                _syncDelegate();
            }
            return null!; // No exception was thrown
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception)
        {
            return null!; // Wrong type - will be handled in AssertAsync
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
        return await CaptureExceptionForReturnAsync();
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
    public ExceptionAssertion(Func<Task> asyncDelegate, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(asyncDelegate, assertionMethod, callerExpression)
    {
    }

    public ExceptionAssertion(Action syncDelegate, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(syncDelegate, assertionMethod, callerExpression)
    {
    }

    public ExceptionAssertion(Func<Task> asyncDelegate, Type expectedType, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(asyncDelegate, expectedType, assertionMethod, callerExpression)
    {
    }

    public ExceptionAssertion(Action syncDelegate, Type expectedType, string assertionMethod = "ThrowsException", string callerExpression = "action")
        : base(syncDelegate, expectedType, assertionMethod, callerExpression)
    {
    }

    public new ExceptionAssertion WithInnerException()
    {
        return (ExceptionAssertion)base.WithInnerException();
    }
}