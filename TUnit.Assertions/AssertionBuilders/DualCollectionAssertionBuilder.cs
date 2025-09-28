using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion builder for delegates that return collections (Func&lt;TCollection&gt;, Func&lt;Task&lt;TCollection&gt;&gt;)
/// Provides value assertions, collection assertions AND behavior assertions
/// </summary>
public class DualCollectionAssertionBuilder<TCollection, TElement> : CollectionAssertionBuilder<TCollection, TElement>
    where TCollection : IEnumerable<TElement>
{
    // New And property that returns the correct type for fluent chaining
    public new DualCollectionAssertionBuilder<TCollection, TElement> And => this;

    public DualCollectionAssertionBuilder(Func<TCollection> funcValue, string? expression = null)
        : base(() => Task.FromResult(funcValue()), expression)
    {
    }

    public DualCollectionAssertionBuilder(Func<Task<TCollection>> asyncFuncValue, string? expression = null)
        : base(asyncFuncValue, expression)
    {
    }

    public DualCollectionAssertionBuilder(Task<TCollection> task, string? expression = null)
        : base(task, expression)
    {
    }

    public DualCollectionAssertionBuilder(ValueTask<TCollection> valueTask, string? expression = null)
        : base(valueTask, expression)
    {
    }

    // Behavior assertions - for delegates that should throw
    public ExceptionAssertion<TException> Throws<TException>()
        where TException : Exception
    {
        return new ExceptionAssertion<TException>(async () =>
        {
            // Execute the delegate and ignore the result, we're only interested in exceptions
            await _actualValueProvider();
        });
    }

    public ExceptionAssertion Throws(Type exceptionType)
    {
        return new ExceptionAssertion(async () =>
        {
            // Execute the delegate and ignore the result, we're only interested in exceptions
            await _actualValueProvider();
        }, exceptionType);
    }

    public ExceptionAssertion<TException> ThrowsException<TException>()
        where TException : Exception
    {
        return Throws<TException>();
    }

    public ExceptionAssertion ThrowsException()
    {
        return new ExceptionAssertion(async () =>
        {
            // Execute the delegate and ignore the result, we're only interested in exceptions
            await _actualValueProvider();
        });
    }

    public ExceptionAssertion<TException> ThrowsExactly<TException>()
        where TException : Exception
    {
        return new ExceptionAssertion<TException>(async () =>
        {
            // Execute the delegate and ignore the result, we're only interested in exceptions
            await _actualValueProvider();
        });
    }

    public ThrowsNothingAssertion<TCollection> ThrowsNothing()
    {
        return new ThrowsNothingAssertion<TCollection>(async () =>
        {
            // Execute the delegate and return the result
            return await _actualValueProvider();
        });
    }

    // Implicit conversion support for awaiting
    public new TaskAwaiter<TCollection> GetAwaiter()
    {
        return _actualValueProvider().GetAwaiter();
    }

    // Base object methods overrides to prevent accidental usage
    [Obsolete("This is a base `object` method that should not be called.", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}