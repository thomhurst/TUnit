using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an action completes execution within the specified time limit.
/// Stops waiting at the limit; the action itself is not canceled.
/// </summary>
public class CompletesWithinActionAssertion : Assertion<object?>
{
    private readonly TimeSpan _timeout;
    private readonly Action _action;

    public CompletesWithinActionAssertion(
        Action action,
        TimeSpan timeout,
        AssertionContext<object?>? context = null)
        : base(context ?? new AssertionContext<object?>(
            new EvaluationContext<object?>(() => Task.FromResult<(object?, Exception?)>((null, null))),
            new StringBuilder()))
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _timeout = timeout;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<object?> metadata)
    {
        using var cts = new CancellationTokenSource();
        var delay = Task.Delay(_timeout, cts.Token); // before the action, so an invalid timeout does not start it

        try
        {
            var task = Task.Run(() => _action());
            var completedTask = await Task.WhenAny(task, delay);

            if (completedTask != task)
            {
                return AssertionResult.Failed("it took too long to complete");
            }

            await task; // Await to get any exceptions

            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"threw {ex.GetType().FullName}: {ex.Message}", ex);
        }
        finally
        {
            cts.Cancel(); // stops the delay's timer when the work finished first or threw
        }
    }

    protected override string GetExpectation() =>
        $"action to complete within {_timeout.TotalMilliseconds:F0} milliseconds";
}

/// <summary>
/// Asserts that an async function completes execution within the specified time limit.
/// Stops waiting at the limit; the function's task is not canceled.
/// </summary>
public class CompletesWithinAsyncAssertion : Assertion<object?>
{
    private readonly TimeSpan _timeout;
    private readonly Func<Task> _asyncAction;

    public CompletesWithinAsyncAssertion(
        Func<Task> asyncAction,
        TimeSpan timeout,
        AssertionContext<object?>? context = null)
        : base(context ?? new AssertionContext<object?>(
            new EvaluationContext<object?>(() => Task.FromResult<(object?, Exception?)>((null, null))),
            new StringBuilder()))
    {
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        _timeout = timeout;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<object?> metadata)
    {
        using var cts = new CancellationTokenSource();
        var delay = Task.Delay(_timeout, cts.Token); // before the task, so an invalid timeout does not start it

        try
        {
            var task = _asyncAction();
            var completedTask = await Task.WhenAny(task, delay);

            if (completedTask != task)
            {
                return AssertionResult.Failed("it took too long to complete");
            }

            await task; // Await to get any exceptions

            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"threw {ex.GetType().FullName}: {ex.Message}", ex);
        }
        finally
        {
            cts.Cancel(); // stops the delay's timer when the work finished first or threw
        }
    }

    protected override string GetExpectation() =>
        $"action to complete within {_timeout.TotalMilliseconds:F0} milliseconds";
}

/// <summary>
/// Asserts that an async function or a task completes within the specified time limit,
/// and returns its result when awaited.
/// </summary>
/// <typeparam name="TValue">The type of the result</typeparam>
public class CompletesWithinAsyncAssertion<TValue> : Assertion<TValue>
{
    private readonly TimeSpan _timeout;

    internal CompletesWithinAsyncAssertion(
        Func<Task<TValue?>> asyncFunc,
        TimeSpan timeout,
        StringBuilder expressionBuilder)
        : base(CreateContext(asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc)), timeout, expressionBuilder))
    {
        _timeout = timeout;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata) =>
        Task.FromResult(metadata.Exception switch
        {
            null => AssertionResult.Passed,
            TimedOutException => AssertionResult.Failed("it took too long to complete"),
            var ex => AssertionResult.Failed($"threw {ex.GetType().FullName}: {ex.Message}", ex),
        });

    protected override string GetExpectation() =>
        $"action to complete within {_timeout.TotalMilliseconds:F0} milliseconds";

    private static AssertionContext<TValue> CreateContext(
        Func<Task<TValue?>> asyncFunc,
        TimeSpan timeout,
        StringBuilder expressionBuilder) =>
        new(new EvaluationContext<TValue>(() => EvaluateWithinAsync(asyncFunc, timeout)), expressionBuilder);

    private static async Task<(TValue?, Exception?)> EvaluateWithinAsync(Func<Task<TValue?>> asyncFunc, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource();
        var delay = Task.Delay(timeout, cts.Token); // before the task, so an invalid timeout does not start it

        try
        {
            var task = asyncFunc();
            var completedTask = await Task.WhenAny(task, delay);

            return completedTask == task ? (await task, null) : (default, new TimedOutException(timeout));
        }
        catch (Exception ex)
        {
            return (default, ex);
        }
        finally
        {
            cts.Cancel(); // stops the delay's timer when the task finished first or threw
        }
    }

    // Marks a timeout in the evaluation; private, so no awaited task can throw it.
    private sealed class TimedOutException(TimeSpan timeout)
        : TimeoutException($"did not complete within {timeout.TotalMilliseconds:F0} milliseconds");
}
