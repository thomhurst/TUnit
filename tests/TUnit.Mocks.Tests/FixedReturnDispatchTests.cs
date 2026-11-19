using TUnit.Mocks.Matchers;
using TUnit.Mocks.Setup;

namespace TUnit.Mocks.Tests;

public class FixedReturnDispatchTests
{
    public enum Dispatch
    {
        Typed,
        Array,
        PartialTyped,
        PartialArray,
        AutoMock
    }

    [Test]
    [Arguments(Dispatch.Typed)]
    [Arguments(Dispatch.Array)]
    [Arguments(Dispatch.PartialTyped)]
    [Arguments(Dispatch.PartialArray)]
    [Arguments(Dispatch.AutoMock)]
    public async Task Fixed_Struct_Returns_Remain_Independent_Copies(Dispatch dispatch)
    {
        var value = new MutableResult { Value = 42 };
        var engine = CreateEngine(value);
        value.Value = 99;

        var first = Invoke<MutableResult>(engine, dispatch);
        first.Value = 100;

        await Assert.That(Invoke<MutableResult>(engine, dispatch).Value).IsEqualTo(42);
    }

    [Test]
    [Arguments(Dispatch.Typed)]
    [Arguments(Dispatch.Array)]
    [Arguments(Dispatch.PartialTyped)]
    [Arguments(Dispatch.PartialArray)]
    [Arguments(Dispatch.AutoMock)]
    public async Task Fixed_Nullable_Returns_Override_The_Fallback(Dispatch dispatch)
    {
        var empty = CreateEngine<int?>(null);
        var populated = CreateEngine<int?>(42);

        await Assert.That(Invoke<int?>(empty, dispatch, 99)).IsNull();
        await Assert.That(Invoke<int?>(populated, dispatch, 99)).IsEqualTo(42);
    }

    [Test]
    [Arguments(Dispatch.Typed)]
    [Arguments(Dispatch.Array)]
    [Arguments(Dispatch.PartialTyped)]
    [Arguments(Dispatch.PartialArray)]
    [Arguments(Dispatch.AutoMock)]
    public async Task Fixed_Returns_Still_Apply_State_OutRef_And_Event_Side_Effects(Dispatch dispatch)
    {
        var engine = new MockEngine<object>(MockBehavior.Loose);
        var setup = CreateSetup();
        new MethodSetupBuilder<int>(setup).Returns(42);
        setup.TransitionTarget = "done";
        setup.SetOutRefValue(0, 123);
        setup.AddEventRaise(new EventRaiseInfo("Changed", null));
        var raisable = new TestRaisable();
        engine.Raisable = raisable;
        engine.AddSetup(setup);

        var result = Invoke<int>(engine, dispatch);
        var assignments = OutRefContext.Consume();

        await Assert.That(result).IsEqualTo(42);
        await Assert.That(engine.CurrentState).IsEqualTo("done");
        await Assert.That(assignments![0]).IsEqualTo(123);
        await Assert.That(raisable.CallCount).IsEqualTo(1);
    }

    [Test]
    [Arguments(Dispatch.Typed)]
    [Arguments(Dispatch.Array)]
    [Arguments(Dispatch.PartialTyped)]
    [Arguments(Dispatch.PartialArray)]
    [Arguments(Dispatch.AutoMock)]
    public async Task Fixed_Raw_Return_Markers_Publish_And_Clear_On_Failed_Side_Effects(Dispatch dispatch)
    {
        var rawValue = new object();
        var marker = new RawReturn(rawValue);
        var engine = CreateEngine<object>(marker);

        var result = Invoke<object>(engine, dispatch);
        var consumed = RawReturnContext.TryConsume(out var published);

        await Assert.That(result).IsSameReferenceAs(marker);
        await Assert.That(consumed).IsTrue();
        await Assert.That(published).IsSameReferenceAs(rawValue);

        var setup = engine.GetSetups()[0];
        setup.AddEventRaise(new EventRaiseInfo("Changed", null));
        engine.Raisable = new TestRaisable { ThrowOnRaise = true };
        Assert.Throws<InvalidOperationException>(() => Invoke<object>(engine, dispatch));
        var leaked = RawReturnContext.TryConsume(out _);
        await Assert.That(leaked).IsFalse();
    }

    private static MethodSetup CreateSetup()
        => new(0, [AnyMatcher<int>.Instance, AnyMatcher<int>.Instance], "Call");

    private static MockEngine<object> CreateEngine<T>(T value)
    {
        var engine = new MockEngine<object>(MockBehavior.Loose);
        var setup = CreateSetup();
        new MethodSetupBuilder<T>(setup).Returns(value);
        engine.AddSetup(setup);
        return engine;
    }

    private static T Invoke<T>(MockEngine<object> engine, Dispatch dispatch, T fallback = default!)
    {
        switch (dispatch)
        {
            case Dispatch.Typed:
                return engine.HandleCallWithReturn<T, int, int>(0, "Call", 1, 2, fallback);
            case Dispatch.Array:
                return engine.HandleCallWithReturn(0, "Call", [1, 2], fallback);
            case Dispatch.PartialTyped:
                if (!engine.TryHandleCallWithReturn<T, int, int>(0, "Call", 1, 2, fallback, out var typed))
                    throw new InvalidOperationException("Configured call fell through to base.");
                return typed;
            case Dispatch.PartialArray:
                if (!engine.TryHandleCallWithReturn(0, "Call", [1, 2], fallback, out var array))
                    throw new InvalidOperationException("Configured call fell through to base.");
                return array;
            case Dispatch.AutoMock:
                return engine.HandleCallWithReturn<T, int, int>(0, "Call", 1, 2, fallback,
                    _ => throw new InvalidOperationException("Configured call attempted auto-mocking."));
            default:
                throw new ArgumentOutOfRangeException(nameof(dispatch));
        }
    }

    private struct MutableResult
    {
        public int Value;
    }

    private sealed class TestRaisable : IRaisable
    {
        public int CallCount;
        public bool ThrowOnRaise;

        public void RaiseEvent(string eventName, object? args)
        {
            CallCount++;
            if (ThrowOnRaise) throw new InvalidOperationException("Event failed.");
        }
    }
}
