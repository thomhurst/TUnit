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

        // Create a TaskContext for Task-specific assertions
        // DO NOT await the task here - we want to check its state synchronously
        var taskExpressionBuilder = new StringBuilder();
        taskExpressionBuilder.Append(expressionBuilder.ToString());
        var taskEvaluationContext = new EvaluationContext<Task>(() =>
        {
            // Return the task object itself without awaiting it
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
}
