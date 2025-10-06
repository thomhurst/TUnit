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
    /// Creates an assertion for an immediate value.
    /// Example: await Assert.That(42).IsEqualTo(42);
    /// </summary>
    public static ValueAssertion<TValue> That<TValue>(
        TValue value,
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
    /// Example: await Assert.That(GetValueAsync()).IsEqualTo(expected);
    /// </summary>
    public static AsyncFuncAssertion<TValue> That<TValue>(
        Task<TValue> task,
        [CallerArgumentExpression(nameof(task))] string? expression = null)
    {
        return new AsyncFuncAssertion<TValue>(() => task, expression);
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
}
