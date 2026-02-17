using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for Task&lt;TValue&gt;.
/// This is the entry point for: Assert.That(GetValueAsync()) or Assert.That(task)
/// Implements both IAssertionSource&lt;TValue&gt; for result assertions and IAssertionSource&lt;Task&lt;TValue&gt;&gt; for task state assertions.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class TaskAssertion<TValue> : IAssertionSource<TValue>, IDelegateAssertionSource<TValue>, IAssertionSource<Task<TValue?>>
{
    public AssertionContext<TValue> Context { get; }
    AssertionContext<Task<TValue?>> IAssertionSource<Task<TValue?>>.Context => TaskContext;

    private AssertionContext<Task<TValue?>> TaskContext { get; }

    public TaskAssertion(Task<TValue?> task, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");

        // Context for result assertions (e.g., IsEqualTo on the TValue result)
        var evaluationContext = new EvaluationContext<TValue>(async () =>
        {
            try
            {
                var result = await task;
                return (result, null);
            }
            catch (Exception ex)
            {
                return (default(TValue), ex);
            }
        });
        Context = new AssertionContext<TValue>(evaluationContext, expressionBuilder);

        // Context for task state assertions (e.g., IsCompleted on the Task<TValue>)
        // DO NOT await the task here - we want to check its state synchronously
        var taskExpressionBuilder = new StringBuilder();
        taskExpressionBuilder.Append(expressionBuilder.ToString());
        var taskEvaluationContext = new EvaluationContext<Task<TValue?>>(() =>
        {
            // Return the task object itself without awaiting it
            // This allows IsCompleted, IsCanceled, IsFaulted, etc. to check task properties synchronously
            return Task.FromResult<(Task<TValue?>?, Exception?)>((task, null));
        });

        TaskContext = new AssertionContext<Task<TValue?>>(taskEvaluationContext, taskExpressionBuilder);
    }

    /// <summary>
    /// Asserts that the task result is of the specified type and returns an assertion on the casted value.
    /// Example: await Assert.That(GetValueAsync()).IsTypeOf<string>();
    /// </summary>
    public TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; type checking.
    /// Asserts that the task itself is of the specified type.
    /// </summary>
    TypeOfAssertion<Task<TValue?>, TExpected> IAssertionSource<Task<TValue?>>.IsTypeOf<TExpected>()
    {
        TaskContext.ExpressionBuilder.Append($".IsTypeOf<{typeof(TExpected).Name}>()");
        return new TypeOfAssertion<Task<TValue?>, TExpected>(TaskContext);
    }

    public IsAssignableToAssertion<TTarget, TValue> IsAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, TValue>(Context);
    }

    public IsNotAssignableToAssertion<TTarget, TValue> IsNotAssignableTo<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, TValue>(Context);
    }

    public IsAssignableFromAssertion<TTarget, TValue> IsAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, TValue>(Context);
    }

    public IsNotAssignableFromAssertion<TTarget, TValue> IsNotAssignableFrom<TTarget>()
    {
        Context.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, TValue>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; assignability checking.
    /// Asserts that the task itself is assignable to the specified type.
    /// </summary>
    IsAssignableToAssertion<TTarget, Task<TValue?>> IAssertionSource<Task<TValue?>>.IsAssignableTo<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsAssignableTo<{typeof(TTarget).Name}>()");
        return new IsAssignableToAssertion<TTarget, Task<TValue?>>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; assignability checking.
    /// Asserts that the task itself is not assignable to the specified type.
    /// </summary>
    IsNotAssignableToAssertion<TTarget, Task<TValue?>> IAssertionSource<Task<TValue?>>.IsNotAssignableTo<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotAssignableTo<{typeof(TTarget).Name}>()");
        return new IsNotAssignableToAssertion<TTarget, Task<TValue?>>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; assignability checking.
    /// Asserts that the task itself is assignable from the specified type.
    /// </summary>
    IsAssignableFromAssertion<TTarget, Task<TValue?>> IAssertionSource<Task<TValue?>>.IsAssignableFrom<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsAssignableFromAssertion<TTarget, Task<TValue?>>(TaskContext);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; assignability checking.
    /// Asserts that the task itself is not assignable from the specified type.
    /// </summary>
    IsNotAssignableFromAssertion<TTarget, Task<TValue?>> IAssertionSource<Task<TValue?>>.IsNotAssignableFrom<TTarget>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotAssignableFrom<{typeof(TTarget).Name}>()");
        return new IsNotAssignableFromAssertion<TTarget, Task<TValue?>>(TaskContext);
    }

    /// <summary>
    /// Asserts that the task result is NOT of the specified type.
    /// Example: await Assert.That(GetValueAsync()).IsNotTypeOf<int>();
    /// </summary>
    public IsNotTypeOfAssertion<TValue, TExpected> IsNotTypeOf<TExpected>()
    {
        Context.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<TValue, TExpected>(Context);
    }

    /// <summary>
    /// Explicit interface implementation for Task&lt;TValue?&gt; type checking.
    /// Asserts that the task itself is NOT of the specified type.
    /// </summary>
    IsNotTypeOfAssertion<Task<TValue?>, TExpected> IAssertionSource<Task<TValue?>>.IsNotTypeOf<TExpected>()
    {
        TaskContext.ExpressionBuilder.Append($".IsNotTypeOf<{typeof(TExpected).Name}>()");
        return new IsNotTypeOfAssertion<Task<TValue?>, TExpected>(TaskContext);
    }

    /// <summary>
    /// Asserts that the async function throws the specified exception type (or subclass).
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
    /// Asserts that the async function throws exactly the specified exception type (not subclasses).
    /// Instance method to avoid C# type inference issues with extension methods.
    /// Example: await Assert.That(async () => await ThrowingMethodAsync()).ThrowsExactly&lt;InvalidOperationException&gt;();
    /// </summary>
    public ThrowsExactlyAssertion<TException> ThrowsExactly<TException>() where TException : Exception
    {
        Context.ExpressionBuilder.Append($".ThrowsExactly<{typeof(TException).Name}>()");
        var mappedContext = Context.MapException<TException>();
        return new ThrowsExactlyAssertion<TException>(mappedContext);
    }

    // Forwarding methods for Task state assertions
    // These allow calling task assertions directly on TaskAssertion<TValue> without type inference issues

    /// <summary>
    /// Asserts that the task is completed.
    /// </summary>
    public TaskIsCompletedAssertion<Task<TValue>> IsCompleted()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsCompleted();
    }

    /// <summary>
    /// Asserts that the task is not completed.
    /// </summary>
    public TaskIsCompletedAssertion<Task<TValue>> IsNotCompleted()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsNotCompleted();
    }

    /// <summary>
    /// Asserts that the task is canceled.
    /// </summary>
    public TaskIsCanceledAssertion<Task<TValue>> IsCanceled()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsCanceled();
    }

    /// <summary>
    /// Asserts that the task is not canceled.
    /// </summary>
    public TaskIsCanceledAssertion<Task<TValue>> IsNotCanceled()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsNotCanceled();
    }

    /// <summary>
    /// Asserts that the task is faulted.
    /// </summary>
    public TaskIsFaultedAssertion<Task<TValue>> IsFaulted()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsFaulted();
    }

    /// <summary>
    /// Asserts that the task is not faulted.
    /// </summary>
    public TaskIsFaultedAssertion<Task<TValue>> IsNotFaulted()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsNotFaulted();
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Asserts that the task completed successfully.
    /// </summary>
    public TaskIsCompletedSuccessfullyAssertion<Task<TValue>> IsCompletedSuccessfully()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsCompletedSuccessfully();
    }

    /// <summary>
    /// Asserts that the task did not complete successfully.
    /// </summary>
    public TaskIsCompletedSuccessfullyAssertion<Task<TValue>> IsNotCompletedSuccessfully()
    {
        return ((IAssertionSource<Task<TValue>>)this).IsNotCompletedSuccessfully();
    }
#endif
}
