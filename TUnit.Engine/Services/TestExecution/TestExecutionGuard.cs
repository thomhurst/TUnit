using System.Collections.Concurrent;

namespace TUnit.Engine.Services.TestExecution;

/// <summary>
/// Prevents duplicate test execution using thread-safe mechanisms.
/// Single Responsibility: Execution deduplication.
/// </summary>
internal sealed class TestExecutionGuard
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _executingTests = new();

    public async Task<bool> TryStartExecutionAsync(string testId, Func<Task> executionFunc)
    {
        var tcs = new TaskCompletionSource<bool>();
        var existingTcs = _executingTests.GetOrAdd(testId, tcs);
        
        if (existingTcs != tcs)
        {
            // Another thread is already executing this test, wait for it
            await existingTcs.Task.ConfigureAwait(false);
            return false; // Test was executed by another thread
        }

        try
        {
            // We got the lock, execute the test
            await executionFunc().ConfigureAwait(false);
            tcs.SetResult(true);
            return true; // We executed the test
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