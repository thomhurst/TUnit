using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion builder for void-returning delegates (Action, Func&lt;Task&gt;)
/// Only provides behavior assertions like ThrowsException, ThrowsNothing
/// </summary>
public class DelegateAssertionBuilder<TDelegate> : AssertionBuilder
    where TDelegate : Delegate
{
    protected readonly Func<Task<TDelegate>> _delegateProvider;
    protected readonly string? _expression;

    internal Func<Task<TDelegate>> DelegateProvider => _delegateProvider;

    // Fluent chaining property - returns self for readability
    public DelegateAssertionBuilder<TDelegate> And => this;

    public DelegateAssertionBuilder(TDelegate delegateValue, string? expression = null)
        : this(() => Task.FromResult(delegateValue), expression)
    {
    }

    public DelegateAssertionBuilder(Func<TDelegate> delegateProvider, string? expression = null)
        : this(() => Task.FromResult(delegateProvider()), expression)
    {
    }

    public DelegateAssertionBuilder(Func<Task<TDelegate>> asyncDelegateProvider, string? expression = null)
    {
        _delegateProvider = asyncDelegateProvider;
        _expression = expression;
    }

    public DelegateAssertionBuilder(Task<TDelegate> task, string? expression = null)
        : this(() => task, expression)
    {
    }

    public DelegateAssertionBuilder(ValueTask<TDelegate> valueTask, string? expression = null)
        : this(() => valueTask.AsTask(), expression)
    {
    }

    // Exception assertions - for delegates that should throw
    public ExceptionAssertion<TException> Throws<TException>()
        where TException : Exception
    {
        return new ExceptionAssertion<TException>(async () =>
        {
            var delegateValue = await _delegateProvider();

            // Execute the delegate based on its type
            if (delegateValue is Action action)
            {
                action();
            }
            else if (delegateValue is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use Throws with {typeof(TDelegate).Name}. Expected Action or Func<Task>.");
            }
        });
    }

    public ExceptionAssertion Throws(Type exceptionType)
    {
        return new ExceptionAssertion(async () =>
        {
            var delegateValue = await _delegateProvider();

            if (delegateValue is Action action)
            {
                action();
            }
            else if (delegateValue is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use Throws with {typeof(TDelegate).Name}. Expected Action or Func<Task>.");
            }
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
            var delegateValue = await _delegateProvider();

            if (delegateValue is Action action)
            {
                action();
            }
            else if (delegateValue is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use ThrowsException with {typeof(TDelegate).Name}. Expected Action or Func<Task>.");
            }
        });
    }

    public ExceptionAssertion<TException> ThrowsExactly<TException>()
        where TException : Exception
    {
        return new ExceptionAssertion<TException>(async () =>
        {
            var delegateValue = await _delegateProvider();

            if (delegateValue is Action action)
            {
                action();
            }
            else if (delegateValue is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use ThrowsExactly with {typeof(TDelegate).Name}. Expected Action or Func<Task>.");
            }
        });
    }

    public ThrowsNothingAssertion<object?> ThrowsNothing()
    {
        return new ThrowsNothingAssertion<object?>(async () =>
        {
            var delegateValue = await _delegateProvider();

            if (delegateValue is Action action)
            {
                action();
                return null;
            }
            else if (delegateValue is Func<Task> asyncFunc)
            {
                await asyncFunc();
                return null;
            }
            else
            {
                throw new InvalidOperationException($"Cannot use ThrowsNothing with {typeof(TDelegate).Name}. Expected Action or Func<Task>.");
            }
        });
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

    // Abstract base class implementations
    public override TaskAwaiter GetAwaiter()
    {
        return ((Task)_delegateProvider()).GetAwaiter();
    }

    public override ValueTask<AssertionData> GetAssertionData()
    {
        return new ValueTask<AssertionData>(new AssertionData(null, null, _expression, DateTimeOffset.Now, DateTimeOffset.Now));
    }

    public override ValueTask ProcessAssertionsAsync(AssertionData data)
    {
        return new ValueTask();
    }
}