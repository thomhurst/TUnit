using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6688;

public class TimeoutCancellationExceptionTests
{
    [Test]
    [Timeout(50)]
    [EngineTest(ExpectedResult.Failure)]
    public async Task Custom_Cancellation_Message(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            // Simulate Aspire gathering resource diagnostics after cancellation.
            await Task.Delay(50);
            throw new OperationCanceledException("Failed due to XYZ", ex.CancellationToken);
        }
    }
}
