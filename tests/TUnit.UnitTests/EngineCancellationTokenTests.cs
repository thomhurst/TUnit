using TUnit.Core;

namespace TUnit.UnitTests;

public class EngineCancellationTokenTests
{
    [Test]
    public async Task Initialise_DoesNotThrow_WhenCancelKeyPressIsUnsupported()
    {
        using var token = new UnsupportedCancelKeyPressToken();

        await Assert.That(() => token.Initialise()).ThrowsNothing();
        await Assert.That(token.SubscribeCalls).IsEqualTo(1);
    }

    [Test]
    public async Task PlatformToken_StillCancels_WhenCancelKeyPressIsUnsupported()
    {
        using var platform = new CancellationTokenSource();
        using var token = new UnsupportedCancelKeyPressToken();
        token.Initialise(platform.Token);

        platform.Cancel();

        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task Dispose_DoesNotUnsubscribe_WhenCancelKeyPressIsUnsupported()
    {
        var token = new UnsupportedCancelKeyPressToken();
        token.Initialise();

        token.Dispose();

        await Assert.That(token.UnsubscribeCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_Unsubscribes_WhenCancelKeyPressIsSupported()
    {
        var token = new RecordingCancelKeyPressToken();
        token.Initialise();

        token.Dispose();

        await Assert.That(token.SubscribeCalls).IsEqualTo(1);
        await Assert.That(token.UnsubscribeCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Initialise_PropagatesOtherSubscriptionFailures()
    {
        using var token = new FailingCancelKeyPressToken();

        await Assert.That(() => token.Initialise()).Throws<InvalidOperationException>();
    }

#if NET
    [Test]
    public async Task TerminationSignal_Arms_Forceful_Exit_Before_Cancellation_Callbacks_Run()
    {
        // Cancellation callbacks (After hooks) run synchronously inside Cancel(); a blocking one
        // must not delay the deadline that bounds shutdown.
        using var token = new RecordingForcefulExitToken();
        bool? armedWhenCallbackRan = null;
        token.Token.Register(() => armedWhenCallbackRan = token.ForcefulExitArmed);

        var handled = token.OnTerminationSignal();

        await Assert.That(handled).IsTrue();
        await Assert.That(armedWhenCallbackRan).IsEqualTo(true);
        await Assert.That(token.TerminationSignalReceived).IsTrue();
        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
    }

    [Test]
    public async Task TerminationSignal_After_Dispose_Neither_Throws_Nor_Arms_Forceful_Exit()
    {
        // The runtime may dispatch a signal it captured before Dispose unregistered the handler.
        // Unhandled, so OnSigterm leaves the runtime's default termination in place.
        var token = new RecordingForcefulExitToken();
        token.Dispose();

        var handled = token.OnTerminationSignal();

        await Assert.That(handled).IsFalse();
        await Assert.That(token.ForcefulExitArmed).IsFalse();
    }
#endif

    [Test]
    public async Task PlatformCancellation_Does_Not_Arm_Forceful_Exit()
    {
        using var platform = new CancellationTokenSource();
        using var token = new RecordingForcefulExitToken();
        token.Initialise(platform.Token);

        platform.Cancel();

        await Assert.That(token.Token.IsCancellationRequested).IsTrue();
        await Assert.That(token.ForcefulExitArmed).IsFalse();
    }

    private sealed class RecordingForcefulExitToken : RecordingCancelKeyPressToken
    {
        public bool ForcefulExitArmed { get; private set; }

        internal override void ArmForcefulExit() => ForcefulExitArmed = true;
    }

    private class RecordingCancelKeyPressToken : EngineCancellationToken
    {
        public int SubscribeCalls { get; private set; }

        public int UnsubscribeCalls { get; private set; }

        internal override void SubscribeCancelKeyPress() => SubscribeCalls++;

        internal override void UnsubscribeCancelKeyPress() => UnsubscribeCalls++;
    }

    private sealed class UnsupportedCancelKeyPressToken : RecordingCancelKeyPressToken
    {
        internal override void SubscribeCancelKeyPress()
        {
            base.SubscribeCancelKeyPress();
            throw new PlatformNotSupportedException();
        }
    }

    private sealed class FailingCancelKeyPressToken : EngineCancellationToken
    {
        internal override void SubscribeCancelKeyPress() => throw new InvalidOperationException();
    }
}
