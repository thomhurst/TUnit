using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion builder for value-returning delegates (Func&lt;T&gt;, Func&lt;Task&lt;T&gt;&gt;)
/// Provides both value assertions (from base class) AND behavior assertions
/// </summary>
public class DualAssertionBuilder<T> : ValueAssertionBuilder<T>
{
    // New And property that returns the correct type for fluent chaining
    public new DualAssertionBuilder<T> And => this;

    public DualAssertionBuilder(Func<T> funcValue, string? expression = null)
        : base(() => Task.FromResult(funcValue()), expression)
    {
    }

    public DualAssertionBuilder(Func<Task<T>> asyncFuncValue, string? expression = null)
        : base(asyncFuncValue, expression)
    {
    }

    public DualAssertionBuilder(Task<T> task, string? expression = null)
        : base(task, expression)
    {
    }

    public DualAssertionBuilder(ValueTask<T> valueTask, string? expression = null)
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

    public ThrowsNothingAssertion<T> ThrowsNothing()
    {
        return new ThrowsNothingAssertion<T>(async () =>
        {
            // Execute the delegate and return the result
            return await _actualValueProvider();
        });
    }

    // Implicit conversion support for awaiting
    public new TaskAwaiter<T> GetAwaiter()
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