using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions;

/// <summary>
/// Entry point for all assertions.
/// Provides Assert.That() overloads for different source types.
/// </summary>
public static class Assert
{
    /// <summary>
    /// Creates an assertion for a Dictionary value.
    /// This overload enables better type inference for dictionary operations like ContainsKey.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
        Dictionary<TKey, TValue> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where TKey : notnull
    {
        return new DictionaryAssertion<TKey, TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for an ImmutableDictionary value.
    /// This overload enables better type inference for dictionary operations like ContainsKey.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
        System.Collections.Immutable.ImmutableDictionary<TKey, TValue> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where TKey : notnull
    {
        return new DictionaryAssertion<TKey, TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for a ReadOnlyDictionary value.
    /// This overload enables better type inference for dictionary operations like ContainsKey.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
        System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
        where TKey : notnull
    {
        return new DictionaryAssertion<TKey, TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for an IReadOnlyDictionary value.
    /// This overload enables better type inference for dictionary operations like ContainsKey.
    /// Example: await Assert.That(dict).ContainsKey("key");
    /// </summary>
    public static DictionaryAssertion<TKey, TValue> That<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new DictionaryAssertion<TKey, TValue>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for an array.
    /// This overload enables better type inference for collection operations like IsInOrder, All, ContainsOnly.
    /// Example: await Assert.That(array).IsInOrder();
    /// </summary>
    public static CollectionAssertion<TItem> That<TItem>(
        TItem[] value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new CollectionAssertion<TItem>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for a List.
    /// This overload enables better type inference for collection operations like IsInOrder, All, ContainsOnly.
    /// Example: await Assert.That(list).IsInOrder();
    /// </summary>
    public static CollectionAssertion<TItem> That<TItem>(
        List<TItem> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new CollectionAssertion<TItem>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for an IEnumerable.
    /// This overload enables better type inference for collection operations like IsInOrder, All, ContainsOnly.
    /// Example: await Assert.That(enumerable).IsInOrder();
    /// </summary>
    public static CollectionAssertion<TItem> That<TItem>(
        IEnumerable<TItem> value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new CollectionAssertion<TItem>(value, expression);
    }

    /// <summary>
    /// Creates an assertion for a non-generic IEnumerable.
    /// Use ValueAssertion to preserve reference equality for IsSameReferenceAs checks.
    /// For collection-specific assertions, cast to IEnumerable<object> first.
    /// Example: await Assert.That(nonGenericCollection).IsSameReferenceAs(other);
    /// </summary>
    public static ValueAssertion<System.Collections.IEnumerable> That(
        System.Collections.IEnumerable value,
        [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        return new ValueAssertion<System.Collections.IEnumerable>(value, expression);
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
        Func<TValue> func,
        [CallerArgumentExpression(nameof(func))] string? expression = null)
    {
        return new FuncAssertion<TValue>(func, expression);
    }

    /// <summary>
    /// Creates an assertion for an asynchronous function.
    /// Example: await Assert.That(async () => await GetValueAsync()).IsEqualTo(expected);
    /// </summary>
    public static AsyncFuncAssertion<TValue> That<TValue>(
        Func<Task<TValue>> func,
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
        Task<TValue> task,
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
        catch (TException ex)
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
        catch (Exception ex) when (exceptionType.IsInstanceOfType(ex))
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
    public static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            throw new AssertionException($"Expected {typeof(TException).Name} but no exception was thrown");
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

    /// <summary>
    /// Asserts that the async action throws the specified exception type with the expected parameter name (for ArgumentException types) and returns the exception.
    /// Example: var exception = await Assert.ThrowsAsync&lt;ArgumentNullException&gt;("paramName", async () => await ThrowingMethodAsync());
    /// </summary>
    public static async Task<TException> ThrowsAsync<TException>(string parameterName, Func<Task> action)
        where TException : ArgumentException
    {
        var exception = await ThrowsAsync<TException>(action);
        if (exception.ParamName != parameterName)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} with ParamName '{parameterName}' but got ParamName '{exception.ParamName}'");
        }
        return exception;
    }

    /// <summary>
    /// Asserts that the async action throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(async () => await ThrowingMethodAsync());
    /// </summary>
    public static async Task<Exception> ThrowsAsync(Func<Task> action)
    {
        return await ThrowsAsync<Exception>(action);
    }

    /// <summary>
    /// Asserts that the Task throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(Task.FromException(new Exception()));
    /// </summary>
    public static async Task<Exception> ThrowsAsync(Task task)
    {
        try
        {
            await task;
            throw new AssertionException("Expected an exception but none was thrown");
        }
        catch (Exception ex) when (ex is not AssertionException)
        {
            return ex;
        }
    }

    /// <summary>
    /// Asserts that the ValueTask throws any exception (defaults to Exception).
    /// Example: var exception = await Assert.ThrowsAsync(new ValueTask(Task.FromException(new Exception())));
    /// </summary>
    public static async Task<Exception> ThrowsAsync(ValueTask task)
    {
        try
        {
            await task;
            throw new AssertionException("Expected an exception but none was thrown");
        }
        catch (Exception ex) when (ex is not AssertionException)
        {
            return ex;
        }
    }

    /// <summary>
    /// Asserts that the specified exception type is thrown and returns the exception.
    /// Non-generic version that takes a Type parameter at runtime.
    /// </summary>
    public static async Task<Exception?> ThrowsAsync(Type exceptionType, Func<Task> action)
    {
        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException($"Type {exceptionType.Name} must be an Exception type", nameof(exceptionType));
        }

        try
        {
            await action();
            throw new AssertionException($"Expected {exceptionType.Name} but no exception was thrown");
        }
        catch (Exception ex) when (exceptionType.IsInstanceOfType(ex))
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {exceptionType.Name} but got {ex.GetType().Name}: {ex.Message}");
        }
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
    public static async Task<TException> ThrowsExactlyAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
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
    /// Example: var exception = await Assert.ThrowsExactlyAsync&lt;ArgumentNullException&gt;("paramName", async () => await ThrowingMethodAsync());
    /// </summary>
    public static async Task<TException> ThrowsExactlyAsync<TException>(string parameterName, Func<Task> action)
        where TException : ArgumentException
    {
        var exception = await ThrowsExactlyAsync<TException>(action);
        if (exception.ParamName != parameterName)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} with ParamName '{parameterName}' but got ParamName '{exception.ParamName}'");
        }
        return exception;
    }
}
