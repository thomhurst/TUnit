using TUnit.Core;

namespace TUnit.UnitTests;

public class TestContextCancellationTests
{
    [Test]
    public async Task CleanupDefersSourceDisposalWhileAcceptedCancellationIsInProgress()
    {
        var context = CreateContext();
        context.InitializeTestCancellation(CancellationToken.None);

        var cancellationToken = context.TestCancellationToken;
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCallback = new ManualResetEventSlim();
        using var registration = cancellationToken.Register(() =>
        {
            callbackStarted.TrySetResult();
            releaseCallback.Wait(TimeSpan.FromSeconds(10));
        });

        var cancellationTask = Task.Run(context.Cancel);

        try
        {
            await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            context.DisposeLinkedCancellationTokenSources();

            await Assert.That(() => cancellationToken.WaitHandle).ThrowsNothing();
        }
        finally
        {
            releaseCallback.Set();
            await cancellationTask;
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    [Test]
    public async Task RetrySourcePreservesAcceptedCancellation()
    {
        var context = CreateContext();
        context.InitializeTestCancellation(CancellationToken.None);

        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseCallback = new ManualResetEventSlim();
        using var registration = context.TestCancellationToken.Register(() =>
        {
            callbackStarted.TrySetResult();
            releaseCallback.Wait(TimeSpan.FromSeconds(10));
        });

        var cancellationTask = Task.Run(context.Cancel);

        try
        {
            await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            context.CurrentRetryAttempt = 1;
            context.InitializeTestCancellation(CancellationToken.None);

            await Assert.That(context.IsTestCancellationRequested).IsTrue();
            await Assert.That(context.TestCancellationToken.IsCancellationRequested).IsTrue();
        }
        finally
        {
            releaseCallback.Set();
            await cancellationTask;
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    [Test]
    public async Task RetryWindowAcceptsCancellationBetweenAttempts()
    {
        var context = CreateContext();
        context.InitializeTestCancellation(CancellationToken.None);
        context.CompleteTestCancellation();

        try
        {
            context.OpenTestCancellationForRetry();
            context.Cancel();

            await Assert.That(context.IsTestCancellationRequested).IsTrue();
            await Assert.That(context.TestCancellationToken.IsCancellationRequested).IsTrue();
        }
        finally
        {
            context.DisposeLinkedCancellationTokenSources();
            context.RemoveFromRegistry();
        }
    }

    private static TestContext CreateContext()
    {
        var currentContext = TestContext.Current!;

        return new TestContext(
            nameof(TestContextCancellationTests),
            currentContext.ServiceProvider,
            currentContext.ClassContext,
            new TestBuilderContext
            {
                TestMetadata = currentContext.TestDetails.MethodMetadata
            },
            CancellationToken.None);
    }
}
