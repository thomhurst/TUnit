using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class CancellationTokenAssertionTests
{
    [Test]
    public async Task Test_CancellationToken_CanBeCanceled()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        await Assert.That(token).CanBeCanceled();
    }

    [Test]
    public async Task Test_CancellationToken_CanBeCanceled_WithTimeout()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var token = cts.Token;
        await Assert.That(token).CanBeCanceled();
    }

    [Test]
    public async Task Test_CancellationToken_CannotBeCanceled_None()
    {
        var token = CancellationToken.None;
        await Assert.That(token).CannotBeCanceled();
    }

    [Test]
    public async Task Test_CancellationToken_CannotBeCanceled_Default()
    {
        var token = default(CancellationToken);
        await Assert.That(token).CannotBeCanceled();
    }

    [Test]
    public async Task Test_CancellationToken_IsCancellationRequested()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var token = cts.Token;
        await Assert.That(token).IsCancellationRequested();
    }

    [Test]
    public async Task Test_CancellationToken_IsCancellationRequested_CancelAfter()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        cts.Cancel();
        await Assert.That(token).IsCancellationRequested();
    }

    [Test]
    public async Task Test_CancellationToken_IsNotCancellationRequested()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        await Assert.That(token).IsNotCancellationRequested();
    }

    [Test]
    public async Task Test_CancellationToken_IsNotCancellationRequested_None()
    {
        var token = CancellationToken.None;
        await Assert.That(token).IsNotCancellationRequested();
    }

    [Test]
    public async Task Test_CancellationToken_IsNone()
    {
        var token = CancellationToken.None;
        await Assert.That(token).IsNone();
    }

    [Test]
    public async Task Test_CancellationToken_IsNone_Default()
    {
        var token = default(CancellationToken);
        await Assert.That(token).IsNone();
    }

    [Test]
    public async Task Test_CancellationToken_IsNotNone()
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        await Assert.That(token).IsNotNone();
    }

    [Test]
    public async Task Test_CancellationToken_IsNotNone_WithTimeout()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token = cts.Token;
        await Assert.That(token).IsNotNone();
    }
}
