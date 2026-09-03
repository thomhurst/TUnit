using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class TimeoutHelperTests
{
    [Test]
    public async Task Timeout_Preserves_Exception_Thrown_During_Cancellation()
    {
        const string cancellationMessage = "Failed due to XYZ";

        var exception = await Assert.That(async () =>
            await TimeoutHelper.ExecuteWithTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        // Keep execution incomplete long enough for timeout detection to win before
                        // cancellation diagnostics finish, matching the Aspire failure in #6688.
                        await Task.Delay(50);
                        throw new OperationCanceledException(cancellationMessage, ex.CancellationToken);
                    }
                },
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None))
            .ThrowsExactly<TimeoutException>();

        await Assert.That(exception!.Message).Contains(cancellationMessage);
        await Assert.That(exception.InnerException).IsTypeOf<OperationCanceledException>();
        await Assert.That(exception.InnerException!.Message).IsEqualTo(cancellationMessage);
    }

    [Test]
    public async Task Timeout_Does_Not_Preserve_Routine_Operation_Cancellation()
    {
        var exception = await Assert.That(async () =>
            await TimeoutHelper.ExecuteWithTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                },
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None))
            .ThrowsExactly<TimeoutException>();

        await Assert.That(exception!.InnerException).IsNull();
        await Assert.That(exception.Message).DoesNotContain(nameof(OperationCanceledException));
    }

    [Test]
    public async Task Timeout_Preserves_Custom_Task_Cancellation()
    {
        const string cancellationMessage = "Custom task cancellation diagnostic";
        var diagnosticException = new InvalidOperationException("Inner diagnostic");

        var exception = await Assert.That(async () =>
            await TimeoutHelper.ExecuteWithTimeoutAsync(
                async cancellationToken =>
                {
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        await Task.Delay(50);
                        throw new TaskCanceledException(cancellationMessage, diagnosticException, cancellationToken);
                    }
                },
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None))
            .ThrowsExactly<TimeoutException>();

        var taskCanceledException = await Assert.That(exception!.InnerException).IsTypeOf<TaskCanceledException>();
        await Assert.That(taskCanceledException!.Message).IsEqualTo(cancellationMessage);
        await Assert.That(taskCanceledException.InnerException).IsSameReferenceAs(diagnosticException);
    }
}
