namespace TUnit.Mocks.Tests;

public delegate int Calculator(int a, int b);
public delegate bool TryLookup(string key, out int value);
public delegate void Bump(ref int value);
public delegate Task<bool> TryLookupAsync(string key, out int value);
public delegate Task FillAsync(out string text);

public class DelegateMockTests
{
    [Test]
    public async Task Func_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Invoke(Any()).Returns(42);

        var result = mock.Object("hello");

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Func_Returns_Default_When_No_Setup()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();

        var result = mock.Object("hello");

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Action_Can_Be_Invoked_And_Verified()
    {
        var mock = Mock.OfDelegate<Action<string>>();

        mock.Object("hello");

        mock.Invoke("hello").WasCalled(Times.Once);
    }

    [Test]
    public async Task Custom_Delegate_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Calculator>();
        mock.Invoke(Any(), Any()).Returns(100);

        var result = mock.Object(3, 5);

        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task Func_Throws_When_Configured()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Invoke(Any()).Throws<InvalidOperationException>();

        var act = () => mock.Object("test");

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Func_Callback_Fires()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        var callbackFired = false;

        mock.Invoke(Any())
            .Callback(() => callbackFired = true)
            .Then()
            .Returns(1);

        mock.Object("x");

        await Assert.That(callbackFired).IsTrue();
    }

    [Test]
    public async Task Func_Arg_Capture_Works()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        var nameArg = Any<string>();
        mock.Invoke(nameArg).Returns(1);

        mock.Object("first");
        mock.Object("second");

        await Assert.That(nameArg.Values).Count().IsEqualTo(2);
        await Assert.That(nameArg.Values[0]).IsEqualTo("first");
        await Assert.That(nameArg.Values[1]).IsEqualTo("second");
    }

    [Test]
    public async Task Func_Verify_WasCalled()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Invoke(Any()).Returns(1);

        mock.Object("a");
        mock.Object("b");

        mock.Invoke(Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Action_Strict_Mode_Throws_On_Unconfigured_Call()
    {
        var mock = Mock.OfDelegate<Action<string>>(MockBehavior.Strict);

        var act = () => mock.Object("test");

        await Assert.That(act).Throws<Exceptions.MockStrictBehaviorException>();
    }

    [Test]
    public async Task Func_Implicit_Conversion_Works()
    {
        var mock = Mock.OfDelegate<Func<int, int>>();
        mock.Invoke(Any()).Returns(99);

        Func<int, int> func = mock;
        var result = func(5);

        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    public async Task Func_Returning_Task_Completes_When_No_Setup()
    {
        var mock = Mock.OfDelegate<Func<int, Task>>();

        await mock.Object(1);

        mock.Invoke(1).WasCalled(Times.Once);
    }

    [Test]
    public async Task Func_Returning_Task_Faults_When_Configured_To_Throw()
    {
        var mock = Mock.OfDelegate<Func<int, Task>>();
        mock.Invoke(Any()).Throws<InvalidOperationException>();

        var task = mock.Object(1);

        await Assert.That(task.IsFaulted).IsTrue();
        await Assert.That(async () => await task).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Func_Returning_ValueTask_Completes_When_No_Setup()
    {
        var mock = Mock.OfDelegate<Func<string, ValueTask>>();

        await mock.Object("a");

        mock.Invoke("a").WasCalled(Times.Once);
    }

    [Test]
    public async Task Func_Returning_Task_Of_T_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Func<int, Task<int>>>();
        mock.Invoke(Any()).Returns(42);

        var result = await mock.Object(1);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Func_Returning_Task_Of_T_Returns_Default_When_No_Setup()
    {
        var mock = Mock.OfDelegate<Func<int, Task<string>>>();

        var result = await mock.Object(1);

        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task Func_Returning_Task_ReturnsAsync_Returns_Configured_Task()
    {
        var mock = Mock.OfDelegate<Func<int, Task>>();
        var tcs = new TaskCompletionSource();
        mock.Invoke(Any()).ReturnsAsync(tcs.Task);

        var task = mock.Object(1);

        await Assert.That(task).IsSameReferenceAs(tcs.Task);
    }

    [Test]
    public async Task Func_Returning_Task_Of_T_ReturnsAsync_Returns_Pending_Task()
    {
        var mock = Mock.OfDelegate<Func<int, Task<int>>>();
        var tcs = new TaskCompletionSource<int>();
        mock.Invoke(Any()).ReturnsAsync(tcs.Task);

        var task = mock.Object(1);

        await Assert.That(task.IsCompleted).IsFalse();
        tcs.SetResult(5);
        await Assert.That(await task).IsEqualTo(5);
    }

    [Test]
    public async Task Func_Returning_ValueTask_Of_T_ReturnsAsync_Returns_Pending_Task()
    {
        var mock = Mock.OfDelegate<Func<int, ValueTask<int>>>();
        var tcs = new TaskCompletionSource<int>();
        mock.Invoke(Any()).ReturnsAsync(new ValueTask<int>(tcs.Task));

        var task = mock.Object(1);

        await Assert.That(task.IsCompleted).IsFalse();
        tcs.SetResult(9);
        await Assert.That(await task).IsEqualTo(9);
    }

    [Test]
    public async Task Func_Returning_ValueTask_ReturnsAsync_Returns_Pending_Task()
    {
        var mock = Mock.OfDelegate<Func<ValueTask>>();
        var tcs = new TaskCompletionSource();
        mock.Invoke().ReturnsAsync(new ValueTask(tcs.Task));

        var task = mock.Object();

        await Assert.That(task.IsCompleted).IsFalse();
        tcs.SetResult();
        await task;
    }

    [Test]
    public async Task Func_Returning_ValueTask_Of_T_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Func<int, ValueTask<int>>>();
        mock.Invoke(Any()).Returns(7);

        var result = await mock.Object(1);

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    public async Task Custom_Delegate_Sets_Out_Parameter()
    {
        var mock = Mock.OfDelegate<TryLookup>();
        mock.Invoke("key").Returns(true).SetsOutValue(42);

        var found = mock.Object("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task Custom_Delegate_Sets_Ref_Parameter()
    {
        var mock = Mock.OfDelegate<Bump>();
        mock.Invoke(Any()).SetsRefValue(99);

        var value = 1;
        mock.Object(ref value);

        await Assert.That(value).IsEqualTo(99);
    }

    [Test]
    public async Task Async_Custom_Delegate_Sets_Out_Parameter()
    {
        var mock = Mock.OfDelegate<TryLookupAsync>();
        mock.Invoke("key").Returns(true).SetsOutValue(7);

        var found = await mock.Object("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(7);
    }

    [Test]
    public async Task Async_Void_Custom_Delegate_Sets_Out_Parameter()
    {
        var mock = Mock.OfDelegate<FillAsync>();
        mock.Invoke().SetsOutText("filled");

        await mock.Object(out var text);

        await Assert.That(text).IsEqualTo("filled");
    }
}
