using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public interface IIssue7684Service
{
    void Ping();
    void Send(string message);
    int Get(int id);
    void Convert<T>(T value);
    Task StartAsync(CancellationToken token);
    ValueTask StopAsync(CancellationToken token);
    Task<int> CountAsync(CancellationToken token);
    ReadOnlySpan<byte> GetBytes();
    void Fill(string key, out int value);
    string Name { get; set; }
    string this[int index] { get; set; }
}

public interface IIssue7684Notifier
{
    event EventHandler<string> StatusChanged;
    void Notify(string message);
}

public class Issue7684Worker
{
    public virtual Task StartAsync(CancellationToken token) => Task.CompletedTask;
    public virtual void Send(string message) { }
}

/// <summary>
/// Verifying a void-shaped member goes through a wrapper that registers a setup eagerly. That setup
/// must not survive verification: it was reported by VerifyAll, allowed calls in strict mode and
/// shadowed real setups.
/// </summary>
public class Issue7684Tests
{
    // ─── VerifyAll ignores verification, for every void-shaped member ───────

    [Test]
    public async Task VerifyAll_Ignores_WasCalled_TimesNever_Without_Setup()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Send("x").WasCalled(Times.Never);

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_WasNeverCalled_Without_Setup()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Send("x").WasNeverCalled();
        mock.Send("x").WasNeverCalled("with a message");
        mock.Send("x").WasCalled(Times.Never, "with a message");

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Of_Parameterless_Void()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Ping().WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Of_Generic_Void()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Convert<int>(Any<int>()).WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Of_Ref_Struct_Return()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.GetBytes().WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Of_Property_Setter_With_Value()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Name.Set("x").WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Of_Indexer_Setter()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.SetItem(1, "x").WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Through_Assertion()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        await Assert.That(mock.Send("x")).WasCalled(Times.Never);

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Inside_VerifyInOrder()
    {
        var mock = IIssue7684Service.Mock();
        mock.Send(Any<string>()).Returns();
        mock.Ping().Returns();
        mock.Object.Send("x");
        mock.Object.Ping();

        Mock.VerifyInOrder(() =>
        {
            mock.Send(Any<string>()).WasCalled();
            mock.Ping().WasCalled();
        });

        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(2);
    }

    [Test]
    public async Task VerifyAll_Ignores_Verification_Inside_State_Scope()
    {
        var mock = IIssue7684Service.Mock();

        Mock.InState(mock, "connected", m => m.Send("x").WasNeverCalled());

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task VerifyAll_Passes_After_Task_Setup_Called_And_Verified_On_Interface()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        mock.StartAsync(Any<CancellationToken>()).Returns();

        await mock.Object.StartAsync(CancellationToken.None);

        mock.StartAsync(Any<CancellationToken>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task VerifyAll_Passes_After_Task_Setup_Called_And_Verified_On_Class()
    {
        var mock = Issue7684Worker.Mock(MockBehavior.Strict);
        mock.StartAsync(Any<CancellationToken>()).Returns();

        await mock.Object.StartAsync(CancellationToken.None);

        mock.StartAsync(Any<CancellationToken>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task VerifyAll_Passes_After_ValueTask_Setup_Called_And_Verified()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        mock.StopAsync(Any<CancellationToken>()).Returns();

        await mock.Object.StopAsync(CancellationToken.None);

        mock.StopAsync(Any<CancellationToken>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task VerifyAll_Passes_After_TaskOfT_Setup_Called_And_Verified()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        mock.CountAsync(Any<CancellationToken>()).Returns(1);

        await mock.Object.CountAsync(CancellationToken.None);

        mock.CountAsync(Any<CancellationToken>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task VerifyAll_Passes_After_Sync_Void_Setup_Called_And_Verified()
    {
        var mock = Issue7684Worker.Mock(MockBehavior.Strict);
        mock.Send(Any<string>()).Returns();

        mock.Object.Send("x");

        mock.Send(Any<string>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(1);
    }

    [Test]
    public async Task VerifyAll_Passes_After_Bare_Setup_Called_And_Verified()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        mock.Send(Any<string>());

        mock.Object.Send("x");

        mock.Send(Any<string>()).WasCalled(Times.Once);
        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(1);
    }

    [Test]
    public async Task Failed_Verification_Leaves_No_Setup()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        Assert.Throws<MockVerificationException>(() => mock.Send("x").WasCalled(Times.Once));

        await AssertNoSetups(mock);
    }

    // ─── Verification leaves the mock's behavior alone ──────────────────────

    [Test]
    public async Task Strict_Mock_Still_Rejects_Call_After_Verifying_Method()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Send("x").WasNeverCalled();

        await Assert.That(() => mock.Object.Send("x")).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Strict_Mock_Still_Rejects_Call_After_Verifying_Parameterless_Void()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Ping().WasNeverCalled();

        await Assert.That(() => mock.Object.Ping()).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Strict_Mock_Still_Rejects_Call_After_Verifying_Property_Setter()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.Name.Set("x").WasNeverCalled();

        await Assert.That(() => mock.Object.Name = "x").Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Strict_Mock_Still_Rejects_Call_After_Verifying_Indexer_Setter()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);

        mock.SetItem(1, "x").WasNeverCalled();

        await Assert.That(() => mock.Object[1] = "x").Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Verification_Does_Not_Shadow_Throws()
    {
        var mock = IIssue7684Service.Mock();
        mock.Send(Any<string>()).Throws<InvalidOperationException>();

        mock.Send(Any<string>()).WasNeverCalled();

        await Assert.That(() => mock.Object.Send("x")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Verification_Does_Not_Shadow_Callback()
    {
        var mock = IIssue7684Service.Mock();
        var count = 0;
        mock.Send(Any<string>()).Callback(() => count++);

        mock.Object.Send("x");
        mock.Send(Any<string>()).WasCalled(Times.Once);
        mock.Object.Send("y");

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task Verification_Does_Not_Shadow_Task_Setup()
    {
        var mock = IIssue7684Service.Mock();
        mock.StartAsync(Any<CancellationToken>()).Throws<InvalidOperationException>();

        mock.StartAsync(Any<CancellationToken>()).WasNeverCalled();

        await Assert.That(async () => await mock.Object.StartAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();
    }

    // ─── Setups that must stay ──────────────────────────────────────────────

    [Test]
    public async Task Bare_Setup_Stays_When_Another_Wrapper_Verifies()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        mock.Send(Any<string>());

        mock.Send(Any<string>()).WasNeverCalled();

        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyAll(mock));
        await Assert.That(ex.Message).Contains("Send");
        mock.Object.Send("x");
    }

    [Test]
    public async Task Stored_Wrapper_Keeps_Configured_Setup_When_Verifying()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var send = mock.Send(Any<string>());
        send.Returns();

        send.WasNeverCalled();
        mock.Object.Send("x");

        send.WasCalled(Times.Once);
        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(1);
    }

    [Test]
    public async Task Stored_Wrapper_Keeps_Invoked_Bare_Setup_When_Verifying()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var ping = mock.Ping();

        mock.Object.Ping();
        ping.WasCalled(Times.Once);
        mock.Object.Ping();

        ping.WasCalled(Times.Exactly(2));
        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(1);
    }

    [Test]
    public async Task Verification_Keeps_Setup_Configured_With_Transition()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var ping = mock.Ping();
        ping.TransitionsTo("next");

        ping.WasNeverCalled();
        mock.Object.Ping();

        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(1);
    }

    [Test]
    public async Task Verification_Keeps_Setup_Configured_With_Event_Raise()
    {
        var mock = IIssue7684Notifier.Mock();
        var raised = new List<string>();
        mock.Object.StatusChanged += (_, status) => raised.Add(status);
        var notify = mock.Notify(Any<string>());
        notify.RaisesStatusChanged("done");

        notify.WasNeverCalled();
        mock.Object.Notify("x");

        await Assert.That(raised).IsEquivalentTo(["done"]);
    }

    [Test]
    public async Task Verification_Keeps_Setup_Configured_With_Out_Value()
    {
        var mock = IIssue7684Service.Mock();
        var fill = mock.Fill("k");
        fill.SetsOutValue(42);

        fill.WasNeverCalled();
        mock.Object.Fill("k", out var value);

        await Assert.That(value).IsEqualTo(42);
    }

    // ─── Stored wrappers stay usable after their setup is withdrawn ─────────

    [Test]
    public async Task Typed_Wrapper_Can_Configure_After_Verifying()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var send = mock.Send("x");
        var count = 0;

        send.WasNeverCalled();
        send.Callback(() => count++);
        mock.Object.Send("x");

        await Assert.That(count).IsEqualTo(1);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task Untyped_Wrapper_Can_Configure_After_Verifying()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var ping = mock.Ping();
        var count = 0;

        ping.WasNeverCalled();
        ping.Callback(() => count++);
        mock.Object.Ping();

        await Assert.That(count).IsEqualTo(1);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task Property_Setter_Wrapper_Can_Configure_After_Verifying()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var set = mock.Name.Set("x");
        var count = 0;

        set.WasNeverCalled();
        set.Callback(() => count++);
        mock.Object.Name = "x";

        await Assert.That(count).IsEqualTo(1);
        Mock.VerifyAll(mock);
    }

    [Test]
    public async Task Stored_Wrapper_Can_Verify_Repeatedly()
    {
        var mock = IIssue7684Service.Mock(MockBehavior.Strict);
        var send = mock.Send("x");

        send.WasNeverCalled();
        send.WasNeverCalled();

        await AssertNoSetups(mock);
    }

    [Test]
    public async Task Stored_Wrapper_Can_Verify_After_Reset()
    {
        var mock = IIssue7684Service.Mock();
        var send = mock.Send("x");
        mock.Object.Send("x");

        Mock.Reset(mock);
        send.WasNeverCalled();

        await AssertNoSetups(mock);
    }

    private static async Task AssertNoSetups<T>(Mock<T> mock) where T : class
    {
        Mock.VerifyAll(mock);
        await Assert.That(Mock.GetDiagnostics(mock).TotalSetups).IsEqualTo(0);
    }
}
