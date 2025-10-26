using System.Diagnostics;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that an action completes execution within the specified time limit.
/// Supports cancellation to avoid waiting for slow operations.
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
            StringBuilderPool.Get()))
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _timeout = timeout;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<object?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(_timeout);

        try
        {
            var task = Task.Run(() => _action());
            var completedTask = await Task.WhenAny(task, Task.Delay(_timeout, cts.Token));

            if (completedTask != task)
            {
                return AssertionResult.Failed("it took too long to complete");
            }

            await task; // Await to get any exceptions
            stopwatch.Stop();

            return AssertionResult.Passed;
        }
        catch (OperationCanceledException)
        {
            return AssertionResult.Failed("it took too long to complete");
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"threw {ex.GetType().FullName}: {ex.Message}");
        }
    }

    protected override string GetExpectation() =>
        $"action to complete within {_timeout.TotalMilliseconds:F0} milliseconds";
}

/// <summary>
/// Asserts that an async function completes execution within the specified time limit.
/// Supports cancellation to avoid waiting for slow operations.
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
            StringBuilderPool.Get()))
    {
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        _timeout = timeout;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<object?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        var stopwatch = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(_timeout);

        try
        {
            var task = _asyncAction();
            var completedTask = await Task.WhenAny(task, Task.Delay(_timeout, cts.Token));

            if (completedTask != task)
            {
                return AssertionResult.Failed("it took too long to complete");
            }

            await task; // Await to get any exceptions
            stopwatch.Stop();

            return AssertionResult.Passed;
        }
        catch (OperationCanceledException)
        {
            return AssertionResult.Failed("it took too long to complete");
        }
        catch (Exception ex)
        {
            return AssertionResult.Failed($"threw {ex.GetType().FullName}: {ex.Message}");
        }
    }

    protected override string GetExpectation() =>
        $"action to complete within {_timeout.TotalMilliseconds:F0} milliseconds";
}
