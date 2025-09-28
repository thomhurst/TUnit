using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Helpers;
using TUnit.Assertions.Wrappers;

namespace TUnit.Assertions;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0002:Assert statements must be awaited")]
public static class Assert
{
    // === SMART TYPE-BASED ROUTING ===

    // === 1. VALUES → ValueAssertionBuilder<T> ===
    // For non-delegate values like numbers, strings, objects
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value,
        [CallerArgumentExpression(nameof(value))] string? doNotPopulateThisValue = null)
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }

    // === 2. COLLECTIONS → CollectionAssertionBuilder<TCollection, TElement> ===
    // Arrays
    public static CollectionAssertionBuilder<TElement[], TElement> That<TElement>(TElement[] array,
        [CallerArgumentExpression(nameof(array))] string? doNotPopulateThisValue = null)
    {
        return new CollectionAssertionBuilder<TElement[], TElement>(array, doNotPopulateThisValue);
    }

    // Lists
    public static CollectionAssertionBuilder<List<T>, T> That<T>(List<T> list,
        [CallerArgumentExpression(nameof(list))] string? doNotPopulateThisValue = null)
    {
        return new CollectionAssertionBuilder<List<T>, T>(list, doNotPopulateThisValue);
    }

    // IEnumerable<T>
    public static CollectionAssertionBuilder<IEnumerable<T>, T> That<T>(IEnumerable<T> enumerable,
        [CallerArgumentExpression(nameof(enumerable))] string? doNotPopulateThisValue = null)
    {
        return new CollectionAssertionBuilder<IEnumerable<T>, T>(enumerable, doNotPopulateThisValue);
    }

    // Untyped IEnumerable (legacy)
    public static CollectionAssertionBuilder<IEnumerable<object>, object> That(IEnumerable enumerable,
        [CallerArgumentExpression(nameof(enumerable))] string? doNotPopulateThisValue = null)
    {
        var wrapper = new UnTypedEnumerableWrapper(enumerable);
        return new CollectionAssertionBuilder<IEnumerable<object>, object>(wrapper, doNotPopulateThisValue);
    }

    // === 3. VOID DELEGATES → DelegateAssertionBuilder<TDelegate> ===
    // Action
    public static DelegateAssertionBuilder<Action> That(Action action,
        [CallerArgumentExpression(nameof(action))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder<Action>(action, doNotPopulateThisValue);
    }

    // Func<Task> (async void)
    public static DelegateAssertionBuilder<Func<Task>> That(Func<Task> asyncAction,
        [CallerArgumentExpression(nameof(asyncAction))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder<Func<Task>>(asyncAction, doNotPopulateThisValue);
    }

    // Task (already executing)
    public static DelegateAssertionBuilder<Func<Task>> That(Task task,
        [CallerArgumentExpression(nameof(task))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder<Func<Task>>(() => task, doNotPopulateThisValue);
    }

    // ValueTask (already executing)
    public static DelegateAssertionBuilder<Func<Task>> That(ValueTask valueTask,
        [CallerArgumentExpression(nameof(valueTask))] string? doNotPopulateThisValue = null)
    {
        return new DelegateAssertionBuilder<Func<Task>>(() => valueTask.AsTask(), doNotPopulateThisValue);
    }

    // === 4. VALUE-RETURNING DELEGATES → DualAssertionBuilder<T> ===
    // Func<T>
    public static DualAssertionBuilder<T> That<T>(Func<T> func,
        [CallerArgumentExpression(nameof(func))] string? doNotPopulateThisValue = null)
    {
        return new DualAssertionBuilder<T>(func, doNotPopulateThisValue);
    }

    // Func<Task<T>>
    public static DualAssertionBuilder<T> That<T>(Func<Task<T>> asyncFunc,
        [CallerArgumentExpression(nameof(asyncFunc))] string? doNotPopulateThisValue = null)
    {
        return new DualAssertionBuilder<T>(asyncFunc, doNotPopulateThisValue);
    }

    // Task<T> (already executing)
    public static DualAssertionBuilder<T> That<T>(Task<T> task,
        [CallerArgumentExpression(nameof(task))] string? doNotPopulateThisValue = null)
    {
        return new DualAssertionBuilder<T>(task, doNotPopulateThisValue);
    }

    // ValueTask<T> (already executing)
    public static DualAssertionBuilder<T> That<T>(ValueTask<T> valueTask,
        [CallerArgumentExpression(nameof(valueTask))] string? doNotPopulateThisValue = null)
    {
        return new DualAssertionBuilder<T>(valueTask, doNotPopulateThisValue);
    }

    // === 5. COLLECTION-RETURNING DELEGATES → DualCollectionAssertionBuilder<TCollection, TElement> ===
    // Func<TElement[]>
    public static DualCollectionAssertionBuilder<TElement[], TElement> That<TElement>(Func<TElement[]> func,
        [CallerArgumentExpression(nameof(func))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<TElement[], TElement>(func, doNotPopulateThisValue);
    }

    // Func<Task<TElement[]>>
    public static DualCollectionAssertionBuilder<TElement[], TElement> That<TElement>(Func<Task<TElement[]>> asyncFunc,
        [CallerArgumentExpression(nameof(asyncFunc))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<TElement[], TElement>(asyncFunc, doNotPopulateThisValue);
    }

    // Func<List<T>>
    public static DualCollectionAssertionBuilder<List<T>, T> That<T>(Func<List<T>> func,
        [CallerArgumentExpression(nameof(func))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<List<T>, T>(func, doNotPopulateThisValue);
    }

    // Func<Task<List<T>>>
    public static DualCollectionAssertionBuilder<List<T>, T> That<T>(Func<Task<List<T>>> asyncFunc,
        [CallerArgumentExpression(nameof(asyncFunc))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<List<T>, T>(asyncFunc, doNotPopulateThisValue);
    }

    // Func<IEnumerable<T>>
    public static DualCollectionAssertionBuilder<IEnumerable<T>, T> That<T>(Func<IEnumerable<T>> func,
        [CallerArgumentExpression(nameof(func))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<IEnumerable<T>, T>(func, doNotPopulateThisValue);
    }

    // Func<Task<IEnumerable<T>>>
    public static DualCollectionAssertionBuilder<IEnumerable<T>, T> That<T>(Func<Task<IEnumerable<T>>> asyncFunc,
        [CallerArgumentExpression(nameof(asyncFunc))] string? doNotPopulateThisValue = null)
    {
        return new DualCollectionAssertionBuilder<IEnumerable<T>, T>(asyncFunc, doNotPopulateThisValue);
    }

    // === MULTIPLE ASSERTIONS ===
    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    // === LEGACY EXCEPTION ASSERTION METHODS ===
    // These methods provide direct exception assertion without going through Assert.That()

    public static ExceptionAssertion ThrowsAsync(Type exceptionType, Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(@delegate, exceptionType);
    }

    public static ExceptionAssertion ThrowsAsync(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(@delegate);
    }

    public static Task<TException> ThrowsAsync<TException>(Func<Task> @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        var assertion = new ExceptionAssertion<TException>(@delegate);
        return assertion.GetExceptionAsync();
    }

    public static ExceptionAssertion ThrowsAsync(Task task,
        [CallerArgumentExpression(nameof(task))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(() => task);
    }

    public static ExceptionAssertion ThrowsAsync(ValueTask valueTask,
        [CallerArgumentExpression(nameof(valueTask))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(() => valueTask.AsTask());
    }

    public static ExceptionAssertion Throws(Type exceptionType, Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(@delegate, exceptionType);
    }

    public static ExceptionAssertion Throws(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
    {
        return new ExceptionAssertion(@delegate);
    }

    public static Task<TException> Throws<TException>(Action @delegate,
        [CallerArgumentExpression(nameof(@delegate))] string? doNotPopulateThisValue = null)
        where TException : Exception
    {
        var assertion = new ExceptionAssertion<TException>(@delegate);
        return assertion.GetExceptionAsync();
    }

    // === ASSERTION FAILURE ===
    public static void Fail(string reason)
    {
        try
        {
            TUnit.Assertions.Fail.Test(reason);
        }
        catch (Exception e)
        {
            throw new AssertionException(e.Message, e);
        }
    }
}