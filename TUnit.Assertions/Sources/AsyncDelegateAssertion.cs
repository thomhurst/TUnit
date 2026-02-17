using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous delegates (Func&lt;Task&gt;).
/// This is the entry point for: Assert.That(async () => await SomeMethodAsync())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Also implements IAssertionSource&lt;Task&gt; to enable Task property assertions like IsCompleted().
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class AsyncDelegateAssertion : IAssertionSource<object?>, IDelegateAssertionSource<object?>, IAssertionSource<Task>
{
    public AssertionContext<object?> Context { get; }
    AssertionContext<Task> IAssertionSource<Task>.Context => TaskContext;

    private AssertionContext<Task> TaskContext { get; }
    internal Func<Task> AsyncAction { get; }

    public AsyncDelegateAssertion(Func<Task> action, string? expression)
    {
        AsyncAction = action ?? throw new ArgumentNullException(nameof(action));
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<object?>(async () =>
        {
            try
            {
                await action();
                return (null, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        });
        Context = new AssertionContext<object?>(evaluationContext, expressionBuilder);

        // DO NOT await the task here - we want to check its state synchronously
        var taskExpressionBuilder = new StringBuilder();
        taskExpressionBuilder.Append(expressionBuilder.ToString());
        var taskEvaluationContext = new EvaluationContext<Task>(() =>
        {
            // This allows IsCompleted, IsCanceled, IsFaulted, etc. to check task properties synchronously
            var task = action();
            return Task.FromResult<(Task?, Exception?)>((task, null));
        });
        TaskContext = new AssertionContext<Task>(taskEvaluationContext, taskExpressionBuilder);
    }

    // Forwarding methods for Task state assertions
    // These allow calling task assertions directly on AsyncDelegateAssertion without type inference issues

    /// <summary>
    /// Asserts that the task is completed.
    /// </summary>
    public TaskIsCompletedAssertion<Task> IsCompleted()
    {
        return ((IAssertionSource<Task>)this).IsCompleted();
    }

    /// <summary>
    /// Asserts that the task is not completed.
    /// </summary>
    public TaskIsCompletedAssertion<Task> IsNotCompleted()
    {
        return ((IAssertionSource<Task>)this).IsNotCompleted();
    }

    /// <summary>
    /// Asserts that the task is canceled.
    /// </summary>
    public TaskIsCanceledAssertion<Task> IsCanceled()
    {
        return ((IAssertionSource<Task>)this).IsCanceled();
    }

    /// <summary>
    /// Asserts that the task is not canceled.
    /// </summary>
    public TaskIsCanceledAssertion<Task> IsNotCanceled()
    {
        return ((IAssertionSource<Task>)this).IsNotCanceled();
    }

    /// <summary>
    /// Asserts that the task is faulted.
    /// </summary>
    public TaskIsFaultedAssertion<Task> IsFaulted()
    {
        return ((IAssertionSource<Task>)this).IsFaulted();
    }

    /// <summary>
    /// Asserts that the task is not faulted.
    /// </summary>
    public TaskIsFaultedAssertion<Task> IsNotFaulted()
    {
        return ((IAssertionSource<Task>)this).IsNotFaulted();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asserts that the task completed successfully.
    /// </summary>
    public TaskIsCompletedSuccessfullyAssertion<Task> IsCompletedSuccessfully()
    {
        return ((IAssertionSource<Task>)this).IsCompletedSuccessfully();
    }

    /// <summary>
    /// Asserts that the task did not complete successfully.
    /// </summary>
    public TaskIsCompletedSuccessfullyAssertion<Task> IsNotCompletedSuccessfully()
    {
        return ((IAssertionSource<Task>)this).IsNotCompletedSuccessfully();
    }
#endif

    /// <summary>
    /// Asserts that the value is of the specified type and returns an assertion on the casted value.
    /// Example: await Assert.That(async () => await SomeMethodAsync()).IsTypeOf<string>();
    /// </summary>
    public TypeOfAssertion<object?, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<object?, TExpected>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task type checking.
    /// Asserts that the task itself is of the specified type.
    /// </summary>
    TypeOfAssertion<Task, TExpected> IAssertionSource<Task>.IsTypeOf<TExpected>()
    {
        TaskContext.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<Task, TExpected>(TaskContext);
    }

    public IsAssignableToAssertion<TTarget, object?> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, object?>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, object?> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, object?>(Context);
    }

    public IsAssignableFromAssertion<TTarget, object?> IsAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, object?>(Context);
    }

    public IsNotAssignableFromAssertion<TTarget, object?> IsNotAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, object?>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task assignability checking.
    /// Asserts that the task itself is assignable to the specified type.
    /// </summary>
    IsAssignableToAssertion<TTarget, Task> IAssertionSource<Task>.IsAssignableTo<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, Task>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task assignability checking.
    /// Asserts that the task itself is not assignable to the specified type.
    /// </summary>
    IsNotAssignableToAssertion<TTarget, Task> IAssertionSource<Task>.IsNotAssignableTo<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, Task>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task assignability checking.
    /// Asserts that the task itself is assignable from the specified type.
    /// </summary>
    IsAssignableFromAssertion<TTarget, Task> IAssertionSource<Task>.IsAssignableFrom<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, Task>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task assignability checking.
    /// Asserts that the task itself is not assignable from the specified type.
    /// </summary>
    IsNotAssignableFromAssertion<TTarget, Task> IAssertionSource<Task>.IsNotAssignableFrom<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, Task>(TaskContext);
    }

    /// <summary>
    /// Asserts that the value is NOT of the specified type.
    /// Example: await Assert.That(async () => await SomeMethodAsync()).IsNotTypeOf<int>();
    /// </summary>
    public IsNotTypeOfAssertion<object?, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<object?, TExpected>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task type checking.
    /// Asserts that the task itself is NOT of the specified type.
    /// </summary>
    IsNotTypeOfAssertion<Task, TExpected> IAssertionSource<Task>.IsNotTypeOf<TExpected>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<Task, TExpected>(TaskContext);
    }

    /// <summary>
    /// Asserts that the async delegate throws the specified exception type (or subclass).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).Throws&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsAssertion<TException> Throws<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".Throws<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the async delegate throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }

    /// <summary>
    /// Asserts that the async delegate throws the specified exception type (runtime Type parameter).
    /// Non-generic version for dynamic exception type checking.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).Throws(typeof(InvalidOperationException));
    /// </summary>
    public Task<Exception?> Throws(Type exceptionType)
    {
        Context.ExpressionBuilder.Append($".Throws({exceptionType.Name})");
        // Delegate to the generic Throws<Exception>() and add runtime type checking
        var assertion = Throws<Exception>();
        return assertion.WithExceptionType(exceptionType);
    }

    /// <summary>
    /// Asserts that the async delegate throws exactly the specified exception type with the expected parameter name.
    /// For ArgumentException types only.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsExactly&lt;ArgumentNullException&gt;("paramName");
    /// </summary>
    public ExceptionParameterNameAssertion<TException> ThrowsExactly<TException>(string parameterName) where TException : ArgumentException
    {
        return ThrowsExactly<TException>().WithParameterName(parameterName);
    }
}
