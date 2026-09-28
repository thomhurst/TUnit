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
