namespace TUnit.Aspire;

internal static class ResourceStartupWaiter
{
    internal static async Task<string?> WaitAsync(
        IReadOnlyList<string> resourceNames,
        Func<string, CancellationToken, Task> waitForReady,
        Func<string, CancellationToken, Task> waitForFailure,
        CancellationToken cancellationToken)
    {
        if (resourceNames.Count == 0)
        {
            return null;
        }

        using var waitsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var readyTask = Task.WhenAll(resourceNames.Select(async name =>
            await waitForReady(name, waitsCts.Token)));
        var failureTasks = resourceNames.Select(async name =>
        {
            await waitForFailure(name, waitsCts.Token);
            return name;
        }).ToArray();
        var anyFailureTask = Task.WhenAny(failureTasks);
        var allWaits = Task.WhenAll(readyTask, Task.WhenAll(failureTasks));

        try
        {
            var completed = await Task.WhenAny(readyTask, anyFailureTask);
            if (completed == anyFailureTask)
            {
                return await await anyFailureTask;
            }

            await readyTask;
            return null;
        }
        finally
        {
            try
            {
                await Task.WhenAll(waitsCts.CancelAsync(), allWaits);
            }
            catch (Exception)
            {
                // The race owns the startup outcome. Join every losing wait (and cancellation
                // callback), observing their faults without replacing that original outcome.
            }
        }
    }
}
