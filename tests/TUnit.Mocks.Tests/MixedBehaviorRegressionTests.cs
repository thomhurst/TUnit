using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public class MixedBehaviorRegressionTests
{
    [Test]
    public async Task Multiple_Callbacks_And_Return_Run_In_One_Repeating_Behavior()
    {
        var observations = new List<string>();
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any())
            .Callback((int a, int b) => observations.Add($"typed:{a}:{b}"))
            .Callback(() => observations.Add("plain"))
            .Returns(42);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(3, 4)).IsEqualTo(42);

        await Assert.That(observations).Count().IsEqualTo(4);
        await Assert.That(observations[0]).IsEqualTo("typed:1:2");
        await Assert.That(observations[1]).IsEqualTo("plain");
        await Assert.That(observations[2]).IsEqualTo("typed:3:4");
        await Assert.That(observations[3]).IsEqualTo("plain");
    }

    [Test]
    public async Task Return_Callback_And_Computed_Return_Use_Last_Return_While_Running_Side_Effects()
    {
        var captured = new List<int>();
        var tailCallbackCount = 0;
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any())
            .Returns(1)
            .Callback((int a, int b) => captured.Add(a + b))
            .Returns((int a, int b) => a * b)
            .Callback(() => tailCallbackCount++);

        await Assert.That(mock.Object.Add(3, 4)).IsEqualTo(12);

        await Assert.That(captured).Count().IsEqualTo(1);
        await Assert.That(captured[0]).IsEqualTo(7);
        await Assert.That(tailCallbackCount).IsEqualTo(1);
    }

    [Test]
    public async Task Callback_Then_Throws_Then_Returns_Sequences_Composite_Steps()
    {
        var observations = new List<string>();
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any())
            .Callback((int a, int b) => observations.Add($"first:{a + b}"))
            .Throws(new InvalidOperationException("first step failed"))
            .Then()
            .Callback((int a, int b) => observations.Add($"second:{a + b}"))
            .Returns(99);

        var exception = Assert.Throws<InvalidOperationException>(() => mock.Object.Add(1, 2));
        await Assert.That(exception.Message).IsEqualTo("first step failed");
        await Assert.That(observations).Count().IsEqualTo(1);
        await Assert.That(observations[0]).IsEqualTo("first:3");

        await Assert.That(mock.Object.Add(4, 5)).IsEqualTo(99);
        await Assert.That(observations).Count().IsEqualTo(2);
        await Assert.That(observations[1]).IsEqualTo("second:9");
    }

    [Test]
    public async Task Throw_Before_Callback_Does_Not_Run_Later_Callback_In_Same_Behavior()
    {
        var laterCallbackRan = false;
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any())
            .Throws(new InvalidOperationException("boom"))
            .Callback(() => laterCallbackRan = true)
            .Then()
            .Returns(7);

        var exception = Assert.Throws<InvalidOperationException>(() => mock.Object.Add(1, 1));
        await Assert.That(exception.Message).IsEqualTo("boom");
        await Assert.That(laterCallbackRan).IsFalse();
        await Assert.That(mock.Object.Add(1, 1)).IsEqualTo(7);
    }

    [Test]
    public async Task Explicit_Then_Separates_Mixed_Steps_And_Last_Step_Repeats()
    {
        var observations = new List<string>();
        var mock = IGreeter.Mock();

        mock.Greet(Any())
            .Callback((string name) => observations.Add($"first:{name}"))
            .Returns((string name) => name.ToUpperInvariant())
            .Then()
            .Callback((string name) => observations.Add($"second:{name}"))
            .Returns("fallback");

        await Assert.That(mock.Object.Greet("alice")).IsEqualTo("ALICE");
        await Assert.That(mock.Object.Greet("bob")).IsEqualTo("fallback");
        await Assert.That(mock.Object.Greet("cara")).IsEqualTo("fallback");

        await Assert.That(observations).Count().IsEqualTo(3);
        await Assert.That(observations[0]).IsEqualTo("first:alice");
        await Assert.That(observations[1]).IsEqualTo("second:bob");
        await Assert.That(observations[2]).IsEqualTo("second:cara");
    }

    [Test]
    public async Task Separate_Setups_Stay_Independent_When_Each_Setup_Has_Mixed_Behaviors()
    {
        var observations = new List<string>();
        var mock = ICalculator.Mock();

        mock.Add(Any(), Any())
            .Callback(() => observations.Add("first setup"))
            .Returns(1);

        mock.Add(Any(), Any())
            .Callback(() => observations.Add("second setup"))
            .Returns(2);

        await Assert.That(mock.Object.Add(10, 20)).IsEqualTo(2);

        await Assert.That(observations).Count().IsEqualTo(1);
        await Assert.That(observations[0]).IsEqualTo("second setup");
    }

    [Test]
    public async Task Async_Task_Callback_And_ReturnsAsync_Run_In_One_Repeating_Behavior()
    {
        var capturedKeys = new List<string>();
        var mock = IAsyncService.Mock();

        mock.GetNameAsync(Any())
            .Callback((string key) => capturedKeys.Add(key))
            .ReturnsAsync((string key) => Task.FromResult($"value:{key}"));

        await Assert.That(await mock.Object.GetNameAsync("one")).IsEqualTo("value:one");
        await Assert.That(await mock.Object.GetNameAsync("two")).IsEqualTo("value:two");

        await Assert.That(capturedKeys).Count().IsEqualTo(2);
        await Assert.That(capturedKeys[0]).IsEqualTo("one");
        await Assert.That(capturedKeys[1]).IsEqualTo("two");
    }

    [Test]
    public async Task Async_ValueTask_Mixed_Sequence_Preserves_Callbacks_And_Returns()
    {
        var capturedInputs = new List<int>();
        var mock = IAsyncService.Mock();

        mock.ComputeValueTaskAsync(Any())
            .Callback((int input) => capturedInputs.Add(input))
            .ReturnsAsync((int input) => new ValueTask<int>(input + 1))
            .Then()
            .Callback((int input) => capturedInputs.Add(input * 10))
            .Returns(500);

        await Assert.That(await mock.Object.ComputeValueTaskAsync(4)).IsEqualTo(5);
        await Assert.That(await mock.Object.ComputeValueTaskAsync(4)).IsEqualTo(500);
        await Assert.That(await mock.Object.ComputeValueTaskAsync(2)).IsEqualTo(500);

        await Assert.That(capturedInputs).Count().IsEqualTo(3);
        await Assert.That(capturedInputs[0]).IsEqualTo(4);
        await Assert.That(capturedInputs[1]).IsEqualTo(40);
        await Assert.That(capturedInputs[2]).IsEqualTo(20);
    }

    [Test]
    public async Task Out_Parameter_Callback_Return_And_Assignment_All_Apply()
    {
        string? capturedKey = null;
        var mock = IDictionary.Mock();

        mock.TryGet(Any())
            .Callback((string key) => capturedKey = key)
            .Returns(true)
            .SetsOutValue("configured");

        var result = mock.Object.TryGet("key", out var value);

        await Assert.That(result).IsTrue();
        await Assert.That(value).IsEqualTo("configured");
        await Assert.That(capturedKey).IsEqualTo("key");
    }

    [Test]
    public async Task Event_Raise_Callback_And_Return_All_Apply()
    {
        var ids = new List<int>();
        string? raisedStatus = null;
        var mock = IProcessService.Mock();
        mock.Object.StatusChanged += (_, status) => raisedStatus = status;

        mock.Process(Any())
            .Callback((int id) => ids.Add(id))
            .Returns(true)
            .RaisesStatusChanged("done");

        var result = mock.Object.Process(123);

        await Assert.That(result).IsTrue();
        await Assert.That(ids).Count().IsEqualTo(1);
        await Assert.That(ids[0]).IsEqualTo(123);
        await Assert.That(raisedStatus).IsEqualTo("done");
    }

    [Test]
    public async Task State_Transition_Callback_And_Returns_All_Apply()
    {
        var observations = new List<string>();
        var mock = IConnection.Mock();
        Mock.SetState(mock, "disconnected");

        Mock.InState(mock, "disconnected", m =>
        {
            m.Connect()
                .Callback(() => observations.Add("connect"))
                .TransitionsTo("connected");
        });

        Mock.InState(mock, "connected", m =>
        {
            m.GetStatus()
                .TransitionsTo("checked")
                .Callback(() => observations.Add("status"))
                .Returns("ONLINE");
        });

        mock.Object.Connect();

        await Assert.That(mock.Object.GetStatus()).IsEqualTo("ONLINE");
        await Assert.That(observations).Count().IsEqualTo(2);
        await Assert.That(observations[0]).IsEqualTo("connect");
        await Assert.That(observations[1]).IsEqualTo("status");
        await Assert.That(MockRegistry.GetEngine(mock).CurrentState).IsEqualTo("checked");
    }
}
