using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions;

/// <summary>
/// Entry point for all assertions.
/// Provides Assert.That() overloads for different source types.
/// </summary>
[SuppressMessage("Usage", "TUnitAssertions0002:Assert statement not awaited")]
public static class Assert
{
    /// <summary>
    /// Creates an assertion for an IReadOnlyDictionary value.
    /// This overload enables better type inference for dictionary operations like ContainsKey.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    [OverloadResolutionPriority(3)]
    public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new DictionaryAssertion<TKey, TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for a string value.
    /// Strings are treated as values, not as character collections, by default.
    /// For character-level assertions, explicitly cast to IEnumerable&lt;char&gt; or char[].
    /// Example: await Assert.That("hello").IsEqualTo("hello");
    /// Example: await Assert.That((IEnumerable&lt;char&gt;)"ABC").Contains('B');
    /// </summary>
    [OverloadResolutionPriority(2)]
    public static ValueAssertion<string> That(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new ValueAssertion<string>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for an IEnumerable (nullable or non-nullable).
    /// This overload enables better type inference for collection operations like IsInOrder, All, ContainsOnly.
    /// Works with any type implementing IEnumerable&lt;T&gt; including List, Array, DbSet, IQueryable, and custom collections.
    /// Example: await Assert.That(enumerable).IsInOrder();
    /// </summary>
    [OverloadResolutionPriority(1)]
    public static CollectionAssertion<TItem> That<TItem>(
        IEnumerable<TItem>? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new CollectionAssertion<TItem>(value!, expression);
    }

    /// <summary>
    /// Creates an assertion for a non-generic IEnumerable.
    /// Automatically casts to IEnumerable&lt;object?&gt; for collection assertions.
    /// Example: await Assert.That(nonGenericCollection).Contains("item");
    /// </summary>
    public static CollectionAssertion<object?> That(
        IEnumerable value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        // Cast to IEnumerable<object?> directly without calling Cast<>() to preserve reference identity
        // This allows IsSameReferenceAs to work correctly on non-generic IEnumerables
        if (value is IEnumerable<object?> genericEnumerable)
        {
            return new CollectionAssertion<object?>(genericEnumerable, expression);
        }

        return new CollectionAssertion<object?>(value.Cast<object?>(), expression);
    }

    /// <summary>
    /// Creates an assertion for an immediate value.
    /// Example: await Assert.That(42).IsEqualTo(42);
    /// </summary>
    public static ValueAssertion<TValue> That<TValue>(
        TValue? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new ValueAssertion<TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for a synchronous function.
    /// Example: await Assert.That(() => GetValue()).IsGreaterThan(10);
    /// </summary>
    public static FuncAssertion<TValue> That<TValue>(
        Func<TValue?> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
    {
        return new FuncAssertion<TValue>(func, expression);
    }

    /// <summary>
    /// Creates an assertion for an asynchronous function.
    /// Example: await Assert.That(async () => await GetValueAsync()).IsEqualTo(expected);
    /// </summary>
    public static AsyncFuncAssertion<TValue> That<TValue>(
        Func<Task<TValue?>> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
    {
        return new AsyncFuncAssertion<TValue>(func, expression);
    }

    /// <summary>
    /// Creates an assertion for a Task that returns a value.
    /// Supports both result assertions (e.g., IsEqualTo) and task state assertions (e.g., IsCompleted).
    /// Example: await Assert.That(GetValueAsync()).IsEqualTo(expected);
    /// Example: await Assert.That(GetValueAsync()).IsCompleted();
    /// </summary>
    public static TaskAssertion<TValue> That<TValue>(
        Task<TValue?> task,
        [CallerArgumentExpression(nameof(task))] string? expression = null)
    {
        return new TaskAssertion<TValue>(task, expression);
    }

    /// <summary>
    /// Creates an assertion for a synchronous delegate (Action).
    /// Example: await Assert.That(() => ThrowingMethod()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public static DelegateAssertion That(
        Action action,
        [CallerArgumentExpression(nameof(action))] string? expression = null)
    {
        return new DelegateAssertion(action, expression);
    }

    /// <summary>
    /// Creates an assertion for an asynchronous delegate (Func&lt;Task&gt;).
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public static AsyncDelegateAssertion That(
        Func<Task> action,
        [CallerArgumentExpression(nameof(action))] string? expression = null)
    {
        return new AsyncDelegateAssertion(action, expression);
    }

    /// <summary>
    /// Creates an assertion for a Task (without return value).
    /// Example: await Assert.That(ThrowingMethodAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public static AsyncDelegateAssertion That(
        Task task,
        [CallerArgumentExpression(nameof(task))] string? expression = null)
    {
        return new AsyncDelegateAssertion(() => task, expression);
    }

    /// <summary>
    /// Creates a scope for grouping multiple assertions together.
    /// All assertions within the scope will execute, and all failures will be collected and reported together.
    /// Example:
    /// using (Assert.Multiple())
    /// {
    ///     await Assert.That(value1).IsEqualTo(expected1);
    ///     await Assert.That(value2).IsEqualTo(expected2);
    /// }
    /// </summary>
    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    /// <summary>
    /// Fails the current test immediately.
    /// If called within Assert.Multiple(), the failure will be accumulated instead of thrown immediately.
    /// Example: Assert.Fail("This should not happen");
    /// </summary>
    /// <param name="reason">The reason why the test failed</param>
    public static void Fail(string reason)
    {
        TUnit.Assertions.Fail.Test(reason);
    }

    /// <summary>
    /// Asserts that the action throws the specified exception type and returns the exception.
    /// Example: var exception = Assert.Throws&lt;InvalidOperationException&gt;(() => ThrowingMethod());
    /// </summary>
    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException($"Expected {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException ex) when (ex is not AssertionException)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Asserts that the action throws an exception of the specified type (or subclass) and returns the exception.
    /// Non-generic version that accepts a Type parameter at runtime.
    /// Example: var exception = Assert.Throws(typeof(InvalidOperationException), () => ThrowingMethod());
    /// </summary>
    public static Exception Throws(Type exceptionType, Action action)
    {
        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException($"Type {exceptionType.Name} must be an Exception type", nameof(exceptionType));
        }

        try
        {
            action();
            throw new AssertionException($"Expected {exceptionType.Name} but no exception was thrown");
        }
        catch (Exception ex) when (exceptionType.IsInstanceOfType(ex) && ex is not AssertionException)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {exceptionType.Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Asserts that the action throws the specified exception type with the expected parameter name (for ArgumentException types) and returns the exception.
    /// Example: var exception = Assert.Throws&lt;ArgumentNullException&gt;("paramName", () => ThrowingMethod());
    /// </summary>
    public static TException Throws<TException>(string parameterName, Action action)
        where TException : ArgumentException
    {
        var exception = Throws<TException>(action);
        if (exception.ParamName != parameterName)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} with ParamName '{parameterName}' but got ParamName '{exception.ParamName}'");
        }
        return exception;
    }

    /// <summary>
    /// Asserts that the async action throws the specified exception type and returns the exception.
    /// Example: var exception = await Assert.ThrowsAsync&lt;InvalidOperationException&gt;(async () => await ThrowingMethodAsync());
    /// </summary>
    public static ThrowsAssertion<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        return That(action).Throws<TException>();
    }

    /// <summary>
    /// Asserts that the async action throws the specified exception type with the expected parameter name (for ArgumentException types).
    /// Uses fluent API - chain .WithParameterName() for parameter validation.
    /// Example: await Assert.ThrowsAsync&lt;ArgumentNullException&gt;("paramName", async () => await ThrowingMethodAsync());
    /// </summary>
    public static ExceptionParameterNameAssertion<TException> ThrowsAsync<TException>(string parameterName, Func<Task> action)
        where TException : ArgumentException
    {
        return That(action).Throws<TException>().WithParameterName(parameterName);
    }

    /// <summary>
    /// Asserts that the async action throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(async () => await ThrowingMethodAsync());
    /// </summary>
    public static ThrowsAssertion<Exception> ThrowsAsync(Func<Task> action)
    {
        return That(action).Throws<Exception>();
    }

    /// <summary>
    /// Asserts that the Task throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(Task.FromException(new Exception()));
    /// </summary>
    public static ThrowsAssertion<Exception> ThrowsAsync(Task task)
    {
        return That(task).Throws<Exception>();
    }

    /// <summary>
    /// Asserts that the ValueTask throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(new ValueTask(Task.FromException(new Exception())));
    /// </summary>
    public static ThrowsAssertion<Exception> ThrowsAsync(ValueTask task)
    {
        return That(task.AsTask()).Throws<Exception>();
    }

    /// <summary>
    /// Asserts that the specified exception type is thrown and returns the exception.
    /// Non-generic version that takes a Type parameter at runtime.
    /// </summary>
    public static Task<Exception?> ThrowsAsync(Type exceptionType, Func<Task> action)
    {
        return That(action).Throws(exceptionType);
    }

    /// <summary>
    /// Asserts that exactly the specified exception type is thrown (not subclasses) and returns the exception.
    /// Example: var exception = Assert.ThrowsExactly&lt;InvalidOperationException&gt;(() => ThrowingMethod());
    /// </summary>
    public static TException ThrowsExactly<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            throw new AssertionException($"Expected exactly {typeof(TException).Name} but no exception was thrown");
        }
        catch (Exception ex) when (ex.GetType() == typeof(TException))
        {
            return (TException)ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected exactly {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Asserts that exactly the specified exception type is thrown (not subclasses) with the expected parameter name (for ArgumentException types) and returns the exception.
    /// Example: var exception = Assert.ThrowsExactly&lt;ArgumentNullException&gt;("paramName", () => ThrowingMethod());
    /// </summary>
    public static TException ThrowsExactly<TException>(string parameterName, Action action)
        where TException : ArgumentException
    {
        var exception = ThrowsExactly<TException>(action);
        if (exception.ParamName != parameterName)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} with ParamName '{parameterName}' but got ParamName '{exception.ParamName}'");
        }
        return exception;
    }

    /// <summary>
    /// Asserts that exactly the specified exception type is thrown (not subclasses) and returns the exception.
    /// Example: var exception = await Assert.ThrowsExactlyAsync&lt;InvalidOperationException&gt;(async () => await ThrowingMethodAsync());
    /// </summary>
    public static ThrowsExactlyAssertion<TException> ThrowsExactlyAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        return That(action).ThrowsExactly<TException>();
    }

    /// <summary>
    /// Asserts that exactly the specified exception type is thrown (not subclasses) with the expected parameter name (for ArgumentException types).
    /// Uses fluent API - chain .WithParameterName() for parameter validation.
    /// Example: await Assert.ThrowsExactlyAsync&lt;ArgumentNullException&gt;("paramName", async () => await ThrowingMethodAsync());
    /// </summary>
    public static ExceptionParameterNameAssertion<TException> ThrowsExactlyAsync<TException>(string parameterName, Func<Task> action)
        where TException : ArgumentException
    {
        return That(action).ThrowsExactly<TException>(parameterName);
    }

    /// <summary>
    /// Asserts that a value is not null (for reference types).
    /// This method properly updates null-state flow analysis, allowing the compiler to treat the value as non-null after this assertion.
    /// Unlike Assert.That(x).IsNotNull() (fluent API), this method changes the compiler's null-state tracking.
    /// Example: Assert.NotNull(myString); // After this, myString is treated as non-null
    /// </summary>
    /// <param name="value">The value to check for null</param>
    /// <param name="expression">The expression being asserted (captured automatically)</param>
    /// <exception cref="AssertionException">Thrown if the value is null</exception>
    public static void NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : class
    {
        if (value is null)
        {
            throw new AssertionException($"Expected {expression ?? "value"} to not be null, but it was null");
        }
    }

    /// <summary>
    /// Asserts that a nullable value type is not null.
    /// This method properly updates null-state flow analysis, allowing the compiler to treat the value as non-null after this assertion.
    /// Example: Assert.NotNull(myNullableInt); // After this, myNullableInt is treated as having a value
    /// </summary>
    /// <param name="value">The nullable value to check</param>
    /// <param name="expression">The expression being asserted (captured automatically)</param>
    /// <exception cref="AssertionException">Thrown if the value is null</exception>
    public static void NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : struct
    {
        if (!value.HasValue)
        {
            throw new AssertionException($"Expected {expression ?? "value"} to not be null, but it was null");
        }
    }

    /// <summary>
    /// Asserts that a value is null (for reference types).
    /// Example: Assert.Null(myString);
    /// </summary>
    /// <param name="value">The value to check for null</param>
    /// <param name="expression">The expression being asserted (captured automatically)</param>
    /// <exception cref="AssertionException">Thrown if the value is not null</exception>
    public static void Null<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : class
    {
        if (value is not null)
        {
            throw new AssertionException($"Expected {expression ?? "value"} to be null, but it was {value}");
        }
    }

    /// <summary>
    /// Asserts that a nullable value type is null.
    /// Example: Assert.Null(myNullableInt);
    /// </summary>
    /// <param name="value">The nullable value to check</param>
    /// <param name="expression">The expression being asserted (captured automatically)</param>
    /// <exception cref="AssertionException">Thrown if the value is not null</exception>
    public static void Null<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : struct
    {
        if (value.HasValue)
        {
            throw new AssertionException($"Expected {expression ?? "value"} to be null, but it was {value.Value}");
        }
    }
}
