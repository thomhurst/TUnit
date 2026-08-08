using TUnit.Core;

namespace TUnit.UnitTests;

public class TestContextCancellationTests
{
    [Test]
    public async Task CleanupDefersSourceDisposalWhileAcceptedCancellationIsInProgress()
    {
        var currentContext = TestContext.Current!;
        var context = new TestContext(
            nameof(CleanupDefersSourceDisposalWhileAcceptedCancellationIsInProgress),
            currentContext.ServiceProvider,
            currentContext.ClassContext,
            new TestBuilderContext
            {
                TestMetadata = currentContext.TestDetails.MethodMetadata
            },
            CancellationToken.None);

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
}
