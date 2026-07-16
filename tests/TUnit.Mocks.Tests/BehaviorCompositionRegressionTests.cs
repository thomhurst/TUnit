using TUnit.Mocks.Arguments;
using TUnit.Mocks.Matchers;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Setup.Behaviors;

namespace TUnit.Mocks.Tests;

public interface IStatefulCommand
{
    int Run(int value);
    void Crash(int value);
    void Ping();
}

public class BehaviorCompositionRegressionTests
{
    [Test]
    public async Task Public_SideEffect_Behavior_Does_Not_Replace_Configured_Return()
    {
        var mock = ICalculator.Mock();
        var sideEffectCount = 0;
        var setup = new MethodSetup(0, [AnyMatcher<int>.Instance, AnyMatcher<int>.Instance], nameof(ICalculator.Add));
        setup.AddBehavior(new CustomReturnBehavior<int>(42));
        setup.AddBehavior(new CustomSideEffectBehavior(() => sideEffectCount++));
        MockRegistry.GetEngine(mock).AddSetup(setup);

        var result = mock.Object.Add(1, 2);

        await Assert.That(typeof(ISideEffectBehavior).IsPublic).IsTrue();
        await Assert.That(result).IsEqualTo(42);
        await Assert.That(sideEffectCount).IsEqualTo(1);
    }

    [Test]
    public async Task Composite_Behavior_Uses_Typed_Dispatch_For_Chained_Behaviors()
    {
        var mock = ICalculator.Mock();
        var captured = 0;

        mock.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((int a, int b) => captured = a + b)
            .Returns(99);

        var result = mock.Object.Add(4, 5);

        await Assert.That(result).IsEqualTo(99);
        await Assert.That(captured).IsEqualTo(9);
    }

    [Test]
    public async Task TransitionsTo_Chains_With_Typed_Callback_And_Returns()
    {
        var mock = IStatefulCommand.Mock();
        var captured = 0;
        Mock.SetState(mock, "ready");

        Mock.InState(mock, "ready", m =>
        {
            m.Run(Arg.Any<int>())
                .TransitionsTo("done")
                .Callback((int value) => captured = value)
                .Returns(123);
        });

        var result = mock.Object.Run(456);

        await Assert.That(result).IsEqualTo(123);
        await Assert.That(captured).IsEqualTo(456);
        await Assert.That(MockRegistry.GetEngine(mock).CurrentState).IsEqualTo("done");
    }

    [Test]
    public async Task TransitionsTo_Does_Not_Advance_State_When_Behavior_Throws()
    {
        var mock = IStatefulCommand.Mock();
        var captured = 0;
        Mock.SetState(mock, "ready");

        Mock.InState(mock, "ready", m =>
        {
            m.Crash(Arg.Any<int>())
                .TransitionsTo("failed")
                .Callback((int value) => captured = value)
                .Throws(new InvalidOperationException("boom"));
        });

        var exception = Assert.Throws<InvalidOperationException>(() => mock.Object.Crash(7));

        await Assert.That(exception.Message).IsEqualTo("boom");
        await Assert.That(captured).IsEqualTo(7);
        await Assert.That(MockRegistry.GetEngine(mock).CurrentState).IsEqualTo("ready");
    }

    [Test]
    public async Task TransitionsTo_Works_Without_Required_State()
    {
        var mock = IStatefulCommand.Mock();

        mock.Ping().TransitionsTo("pinged");

        mock.Object.Ping();

        await Assert.That(MockRegistry.GetEngine(mock).CurrentState).IsEqualTo("pinged");
    }

    private sealed class CustomSideEffectBehavior(Action callback) : IBehavior, ISideEffectBehavior
    {
        public object? Execute(object?[] arguments)
        {
            callback();
            return null;
        }
    }

    private sealed class CustomReturnBehavior<T>(T value) : IBehavior, IArgumentFreeBehavior
    {
        public object? Execute(object?[] arguments) => Execute();

        public object? Execute() => value;
    }

}
