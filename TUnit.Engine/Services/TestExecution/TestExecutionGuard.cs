using System.Collections.Concurrent;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Prevents duplicate test execution using thread-safe mechanisms.
/// Single Responsibility: Execution deduplication.
/// </summary>
internal sealed class TestExecutionGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _executingTests = new();

    public ValueTask<bool> TryStartExecutionAsync(string testId, Func<ValueTask> executionFunc)
    {
        var tcs = new TaskCompletionSource<bool>();
        var existingTcs = _executingTests.GetOrAdd(testId, tcs);

        if (existingTcs != tcs)
        {
            return new ValueTask<bool>(WaitForExistingExecutionAsync(existingTcs));
        }

        return ExecuteAndCompleteAsync(testId, tcs, executionFunc);
    }

    private static async Task<bool> WaitForExistingExecutionAsync(TaskCompletionSource<bool> tcs)
    {
        await tcs.Task.ConfigureAwait(false);
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