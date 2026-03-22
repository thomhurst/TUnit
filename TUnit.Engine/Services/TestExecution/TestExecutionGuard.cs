using System.Collections.Concurrent;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Prevents duplicate test execution using thread-safe mechanisms.
/// Single Responsibility: Execution deduplication.
/// </summary>
internal sealed class TestExecutionGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _executingTests = new();

    public ValueTask TryStartExecutionAsync(string testId, Func<ValueTask> executionFunc)
    {
        // Fast path: check if test is already executing without allocating a TCS
        if (_executingTests.TryGetValue(testId, out var existingTcs))
        {
            return new ValueTask(WaitForExistingExecutionAsync(existingTcs));
        }

        var tcs = new TaskCompletionSource<bool>();
        existingTcs = _executingTests.GetOrAdd(testId, tcs);

        if (existingTcs != tcs)
        {
            return new ValueTask(WaitForExistingExecutionAsync(existingTcs));
        }

        return ExecuteAndCompleteAsync(testId, tcs, executionFunc);
    }

    private static async Task WaitForExistingExecutionAsync(TaskCompletionSource<bool> tcs)
    {
        await tcs.Task.ConfigureAwait(false);
    }

    private async ValueTask ExecuteAndCompleteAsync(string testId, TaskCompletionSource<bool> tcs, Func<ValueTask> executionFunc)
    {
        try
        {
            await executionFunc().ConfigureAwait(false);
            tcs.SetResult(true);
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