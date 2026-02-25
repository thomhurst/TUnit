using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

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
        Mock.SetState(mock, "disconnected");

        Mock.InState(mock, "disconnected", m =>
        {
            m.GetStatus().Returns("OFFLINE");
        });

        Mock.InState(mock, "connected", m =>
        {
            m.GetStatus().Returns("ONLINE");
        });

        IConnection conn = mock.Object;

        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");

        Mock.SetState(mock, "connected");
        await Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");

        Mock.SetState(mock, "disconnected");
        await Assert.That(conn.GetStatus()).IsEqualTo("OFFLINE");
    }

    [Test]
    public async Task TransitionsTo_Changes_State_After_Call()
    {
        var mock = Mock.Of<IConnection>();
        Mock.SetState(mock, "disconnected");

        Mock.InState(mock, "disconnected", m =>
        {
            m.Connect().TransitionsTo("connected");
            m.GetStatus().Returns("OFFLINE");
        });

        Mock.InState(mock, "connected", m =>
        {
            m.Disconnect().TransitionsTo("disconnected");
            m.GetStatus().Returns("ONLINE");
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
        Mock.SetState(mock, "connected");

        Mock.InState(mock, "connected", m =>
        {
            m.Connect().Throws<InvalidOperationException>();
        });

        Mock.InState(mock, "disconnected", m =>
        {
            m.Disconnect().Throws<InvalidOperationException>();
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
        Mock.SetState(mock, "disconnected");

        // Setup without state guard — matches in any state
        mock.GetStatus().Returns("ALWAYS");

        IConnection conn = mock.Object;
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");

        Mock.SetState(mock, "connected");
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");

        Mock.SetState(mock, null);
        await Assert.That(conn.GetStatus()).IsEqualTo("ALWAYS");
    }

    [Test]
    public async Task State_Scoped_Setup_Overrides_Global_When_In_State()
    {
        var mock = Mock.Of<IConnection>();

        // Global setup (no state guard)
        mock.GetStatus().Returns("DEFAULT");

        // State-scoped setup
        Mock.InState(mock, "special", m =>
        {
            m.GetStatus().Returns("SPECIAL");
        });

        IConnection conn = mock.Object;

        // No state set — global matches
        await Assert.That(conn.GetStatus()).IsEqualTo("DEFAULT");

        // State set — scoped setup wins (last-wins semantics, state-scoped was added later)
        Mock.SetState(mock, "special");
        await Assert.That(conn.GetStatus()).IsEqualTo("SPECIAL");

        // Different state — scoped doesn't match, global wins
        Mock.SetState(mock, "other");
        await Assert.That(conn.GetStatus()).IsEqualTo("DEFAULT");
    }

    [Test]
    public async Task Strict_Mode_Throws_For_Unconfigured_Call_In_State()
    {
        var mock = Mock.Of<IConnection>(MockBehavior.Strict);
        Mock.SetState(mock, "disconnected");

        Mock.InState(mock, "disconnected", m =>
        {
            m.GetStatus().Returns("OFFLINE");
        });

        // No setup for Connect in "disconnected" state — strict mode should throw
        IConnection conn = mock.Object;

        var exception = Assert.Throws<MockStrictBehaviorException>(() => conn.Connect());
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Nested_InState_Restores_Previous_State_Scope()
    {
        // Regression test: nested InState calls must save/restore PendingRequiredState
        var mock = Mock.Of<IConnection>();
        Mock.SetState(mock, "outer");

        Mock.InState(mock, "outer", m =>
        {
            m.GetStatus().Returns("OUTER");

            // Nested InState should temporarily switch to "inner" scope
            Mock.InState(mock, "inner", m =>
            {
                m.Connect();
            });

            // After inner InState returns, we should be back in "outer" scope
            m.Disconnect();
        });

        IConnection conn = mock.Object;

        // In "outer" state, GetStatus and Disconnect should work
        await Assert.That(conn.GetStatus()).IsEqualTo("OUTER");
        conn.Disconnect(); // should not throw (setup registered in outer scope)

        // In "inner" state, Connect should work (it was set up in inner scope)
        Mock.SetState(mock, "inner");
        conn.Connect(); // should not throw
    }

    [Test]
    public async Task SetState_Null_Clears_State()
    {
        var mock = Mock.Of<IConnection>();

        // Global (no-state) setup — added first
        mock.GetStatus().Returns("NO_STATE");

        // State-scoped setup — added second, wins when in "connected" state
        Mock.InState(mock, "connected", m =>
        {
            m.GetStatus().Returns("ONLINE");
        });

        Mock.SetState(mock, "connected");

        IConnection conn = mock.Object;
        await Assert.That(conn.GetStatus()).IsEqualTo("ONLINE");

        // Clear state — scoped setup no longer matches, global setup wins
        Mock.SetState(mock, null);
        await Assert.That(conn.GetStatus()).IsEqualTo("NO_STATE");
    }

    [Test]
    public async Task Reset_Clears_State()
    {
        var mock = Mock.Of<IConnection>();
        Mock.SetState(mock, "connected");

        Mock.Reset(mock);

        // After reset, current state should be null
        await Assert.That(Mock.GetEngine(mock).CurrentState).IsNull();
    }

    [Test]
    public async Task Verify_Works_With_State_Scoped_Setups()
    {
        var mock = Mock.Of<IConnection>();
        Mock.SetState(mock, "disconnected");

        Mock.InState(mock, "disconnected", m =>
        {
            m.Connect().TransitionsTo("connected");
        });

        IConnection conn = mock.Object;
        conn.Connect();

        // Verification should still work — the call was recorded
        mock.Connect().WasCalled(Times.Once);
    }
}
