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
                        // Ensure timeout detection wins before cancellation diagnostics finish.
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
}
