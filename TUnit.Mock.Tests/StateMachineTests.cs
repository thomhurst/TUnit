using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Tests;

public interface IConnection
{
    void Connect();
    void Disconnect();
    string GetStatus();
}

public class StateMachineTests
{
    [Test]
    public async Task State_Machine_Returns_Different_Values_Per_State()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("disconnected");

        mock.InState("disconnected", setup =>
        {
            setup.GetStatus().Returns("OFFLINE");
        });

        mock.InState("connected", setup =>
        {
            setup.GetStatus().Returns("ONLINE");
        });

        IConnection conn = mock.Object;

        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");

        mock.SetState("connected");
        await Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");

        mock.SetState("disconnected");
        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
    }

    [Test]
    public async Task TransitionsTo_Changes_State_After_Call()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("disconnected");

        mock.InState("disconnected", setup =>
        {
            setup.Connect().TransitionsTo("connected");
            setup.GetStatus().Returns("OFFLINE");
        });

        mock.InState("connected", setup =>
        {
            setup.Disconnect().TransitionsTo("disconnected");
            setup.GetStatus().Returns("ONLINE");
        });

        IConnection conn = mock.Object;

        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");

        conn.Connect(); // transitions to "connected"
        await Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");

        conn.Disconnect(); // transitions back to "disconnected"
        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
    }

    [Test]
    public async Task State_Scoped_Throws()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("connected");

        mock.InState("connected", setup =>
        {
            setup.Connect().Throws<InvalidOperationException>();
        });

        mock.InState("disconnected", setup =>
        {
            setup.Disconnect().Throws<InvalidOperationException>();
        });

        IConnection conn = mock.Object;

        // In "connected" state, Connect should throw
        var exception = Assert.Throws<InvalidOperationException>(() => conn.Connect());
        await Assert.That(exception).IsNotNull();

        // Disconnect is not configured in "connected" state — loose mode allows it
        conn.Disconnect(); // should not throw (no matching setup, loose mode)
    }

    [Test]
    public async Task No_State_Setups_Match_In_Any_State()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("disconnected");

        // Setup without state guard — matches in any state
        mock.Setup.GetStatus().Returns("ALWAYS");

        IConnection conn = mock.Object;
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");

        mock.SetState("connected");
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");

        mock.SetState(null);
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");
    }

    [Test]
    public async Task State_Scoped_Setup_Overrides_Global_When_In_State()
    {
        var mock = Mock.Of<IConnection>();

        // Global setup (no state guard)
        mock.Setup.GetStatus().Returns("DEFAULT");

        // State-scoped setup
        mock.InState("special", setup =>
        {
            setup.GetStatus().Returns("SPECIAL");
        });

        IConnection conn = mock.Object;

        // No state set — global matches
        await Assert.That(conn.GetStatus()).IsEqualTo("DEFAULT");

        // State set — scoped setup wins (last-wins semantics, state-scoped was added later)
        mock.SetState("special");
        await Assert.That(conn.GetStatus()).IsEqualTo("SPECIAL");

        // Different state — scoped doesn't match, global wins
        mock.SetState("other");
        await Assert.That(conn.GetStatus()).IsEqualTo("DEFAULT");
    }

    [Test]
    public async Task Strict_Mode_Throws_For_Unconfigured_Call_In_State()
    {
        var mock = Mock.Of<IConnection>(MockBehavior.Strict);
        mock.SetState("disconnected");

        mock.InState("disconnected", setup =>
        {
            setup.GetStatus().Returns("OFFLINE");
        });

        // No setup for Connect in "disconnected" state — strict mode should throw
        IConnection conn = mock.Object;

        var exception = Assert.Throws<MockStrictBehaviorException>(() => conn.Connect());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Reset_Clears_State()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("connected");

        mock.Reset();

        // After reset, Engine.CurrentState should be null
        await Assert.That(mock.Engine.CurrentState).IsNull();
    }

    [Test]
    public async Task Verify_Works_With_State_Scoped_Setups()
    {
        var mock = Mock.Of<IConnection>();
        mock.SetState("disconnected");

        mock.InState("disconnected", setup =>
        {
            setup.Connect().TransitionsTo("connected");
        });

        IConnection conn = mock.Object;
        conn.Connect();

        // Verification should still work — the call was recorded
        mock.Verify!.Connect().WasCalled(Times.Once);
    }
}
