namespace TUnit.Mock.Tests;

public delegate int Calculator(int a, int b);

public class DelegateMockTests
{
    [Test]
    public async Task Func_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Setup.Invoke(Arg.Any<string>()).Returns(42);

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

        mock.Verify!.Invoke("hello").WasCalled(Times.Once);
    }

    [Test]
    public async Task Custom_Delegate_Returns_Configured_Value()
    {
        var mock = Mock.OfDelegate<Calculator>();
        mock.Setup.Invoke(Arg.Any<int>(), Arg.Any<int>()).Returns(100);

        var result = mock.Object(3, 5);

        await Assert.That(result).IsEqualTo(100);
    }

    [Test]
    public async Task Func_Throws_When_Configured()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Setup.Invoke(Arg.Any<string>()).Throws<InvalidOperationException>();

        var act = () => mock.Object("test");

        await Assert.That(act).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Func_Callback_Fires()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        var callbackFired = false;

        mock.Setup.Invoke(Arg.Any<string>())
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
        var nameArg = Arg.Any<string>();
        mock.Setup.Invoke(nameArg).Returns(1);

        mock.Object("first");
        mock.Object("second");

        await Assert.That(nameArg.Values).HasCount().EqualTo(2);
        await Assert.That(nameArg.Values[0]).IsEqualTo("first");
        await Assert.That(nameArg.Values[1]).IsEqualTo("second");
    }

    [Test]
    public async Task Func_Verify_WasCalled()
    {
        var mock = Mock.OfDelegate<Func<string, int>>();
        mock.Setup.Invoke(Arg.Any<string>()).Returns(1);

        mock.Object("a");
        mock.Object("b");

        mock.Verify!.Invoke(Arg.Any<string>()).WasCalled(Times.Exactly(2));
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
        mock.Setup.Invoke(Arg.Any<int>()).Returns(99);

        Func<int, int> func = mock;
        var result = func(5);

        await Assert.That(result).IsEqualTo(99);
    }
}
