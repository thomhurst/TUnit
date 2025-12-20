using System.Collections.Concurrent;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Prevents duplicate test execution using thread-safe mechanisms.
/// Single Responsibility: Execution deduplication.
/// </summary>
internal sealed class TestExecutionGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _executingTests = new();

    public ValueTask<bool> TryStartExecutionAsync(string testId, Func<ValueTask> executionFunc, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>();
        var existingTcs = _executingTests.GetOrAdd(testId, tcs);

        if (existingTcs != tcs)
        {
            return new ValueTask<bool>(WaitForExistingExecutionAsync(existingTcs, cancellationToken));
        }

        return ExecuteAndCompleteAsync(testId, tcs, executionFunc);
    }

    private static async Task<bool> WaitForExistingExecutionAsync(TaskCompletionSource<bool> tcs, CancellationToken cancellationToken)
    {
        await tcs.Task.WaitWithCancellationAsync(cancellationToken).ConfigureAwait(false);
        return false;
    }

    private async ValueTask<bool> ExecuteAndCompleteAsync(string testId, TaskCompletionSource<bool> tcs, Func<ValueTask> executionFunc)
    {
        try
        {
            await executionFunc().ConfigureAwait(false);
            tcs.SetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            throw;
        }
        finally
        {
            _executingTests.TryRemove(testId, out _);
        }
    }
}