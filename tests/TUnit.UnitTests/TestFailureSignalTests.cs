using TUnit.Core.Exceptions;

namespace TUnit.UnitTests;

public class TestFailureSignalTests
{
    [Test]
    public async Task FirstReportWinsAndCancelsToken()
    {
        using var signal = new TestFailureSignal(CancellationToken.None);
        var firstException = new InvalidOperationException("first");

        var firstAccepted = signal.TryReport(firstException);
        var secondAccepted = signal.TryReport(new InvalidOperationException("second"));
        var reportedException = signal.Complete();

        await Assert.That(firstAccepted).IsTrue();
        await Assert.That(secondAccepted).IsFalse();
        await Assert.That(signal.CancellationToken.IsCancellationRequested).IsTrue();
        await Assert.That(reportedException).IsSameReferenceAs(firstException);
    }

    [Test]
    public async Task ReportAfterCompletionIsRejected()
    {
        using var signal = new TestFailureSignal(CancellationToken.None);
        signal.Complete();

        var accepted = signal.TryReport(new InvalidOperationException("too late"));

        await Assert.That(accepted).IsFalse();
    }

    [Test]
    public async Task ReasonCreatesFailTestException()
    {
        using var signal = new TestFailureSignal(CancellationToken.None);

        signal.Report("callback failed");

        var exception = signal.Complete();
        await Assert.That(exception).IsTypeOf<FailTestException>();
        await Assert.That(exception!.Message).IsEqualTo("callback failed");
    }

    [Test]
    public async Task CancellationCallbackExceptionsDoNotEscapeReportingThread()
    {
        using var signal = new TestFailureSignal(CancellationToken.None);
        using var registration = signal.CancellationToken.Register(
            static () => throw new InvalidOperationException("cancellation callback failed"));

        signal.Report(new InvalidOperationException("test failed"));

        await Assert.That(signal.Complete()).IsNotNull();
    }
}
