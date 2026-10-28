using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

public class ResourceStartupWaiterTests
{
    [Test]
    [Timeout(10_000)]
    [Arguments("failure", false)]
    [Arguments("failure", true)]
    [Arguments("success", false)]
    [Arguments("success", true)]
    [Arguments("readiness-fault", false)]
    [Arguments("readiness-fault", true)]
    [Arguments("watcher-fault", false)]
    [Arguments("watcher-fault", true)]
    [Arguments("cancellation", false)]
    [Arguments("cancellation", true)]
    public async Task EveryExit_CancelsAndJoinsPendingWaits(string outcome, bool faultOnCleanup, CancellationToken cancellationToken)
    {
        using var startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var trigger = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingReady = new PendingWait(faultOnCleanup);
        var pendingFailure = new PendingWait(faultOnCleanup);
        var originalFailure = new InvalidOperationException("original startup failure");
        var hasPendingReady = outcome is "failure" or "cancellation" or "watcher-fault";

        Task Ready(string name, CancellationToken token)
        {
            if (hasPendingReady)
            {
                return name == "pending" ? pendingReady.RunAsync(token) : Task.Delay(Timeout.Infinite, token);
            }

            return name == "trigger" ? trigger.Task.WaitAsync(token) : Task.CompletedTask;
        }

        Task Failure(string name, CancellationToken token)
        {
            if (name == "pending")
            {
                return pendingFailure.RunAsync(token);
            }

            return outcome is "failure" or "watcher-fault"
                ? trigger.Task.WaitAsync(token)
                : Task.Delay(Timeout.Infinite, token);
        }

        var wait = ResourceStartupWaiter.WaitAsync(["trigger", "pending"], Ready, Failure, startupCancellation.Token);
        try
        {
            if (outcome == "cancellation")
            {
                await startupCancellation.CancelAsync();
            }
            else if (outcome is "readiness-fault" or "watcher-fault")
            {
                trigger.SetException(originalFailure);
            }
            else
            {
                trigger.SetResult();
            }

            // Either the old implementation returns too soon, or cleanup reaches the pending watcher.
            await Task.WhenAny(wait, pendingFailure.CancellationRequested.Task).WaitAsync(cancellationToken);
            await Assert.That(wait.IsCompleted).IsFalse()
                .Because("startup must join the cancelled waits before reporting its outcome");
            await pendingFailure.CancellationRequested.Task.WaitAsync(cancellationToken);
            if (hasPendingReady)
            {
                await pendingReady.CancellationRequested.Task.WaitAsync(cancellationToken);
            }

            pendingReady.Release.SetResult();
            pendingFailure.Release.SetResult();
            if (outcome == "cancellation")
            {
                var failure = await Assert.That(async () => await wait).Throws<OperationCanceledException>();
                await Assert.That(failure!.CancellationToken.IsCancellationRequested).IsTrue();
            }
            else if (outcome is "readiness-fault" or "watcher-fault")
            {
                var failure = await Assert.That(async () => await wait).Throws<InvalidOperationException>();
                await Assert.That(failure).IsSameReferenceAs(originalFailure);
            }
            else
            {
                await Assert.That(await wait).IsEqualTo(outcome == "failure" ? "trigger" : null);
            }

            await Assert.That(pendingFailure.Completed).IsTrue();
            if (hasPendingReady)
            {
                await Assert.That(pendingReady.Completed).IsTrue();
            }
        }
        finally
        {
            await startupCancellation.CancelAsync();
            pendingReady.Release.TrySetResult();
            pendingFailure.Release.TrySetResult();
            try
            {
                await wait;
            }
            catch (Exception)
            {
                // Observe the test operation even when an assertion fails.
            }
        }
    }

    private sealed class PendingWait(bool faultOnCleanup)
    {
        public TaskCompletionSource CancellationRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Completed { get; private set; }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() => CancellationRequested.TrySetResult());
            try
            {
                await Release.Task;
                if (faultOnCleanup)
                {
                    // A losing wait can fault while handling cancellation; cleanup must observe it too.
                    throw new InvalidOperationException("losing wait failed during cleanup");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            finally
            {
                Completed = true;
            }
        }
    }
}
