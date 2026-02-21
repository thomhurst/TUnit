using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Integration tests for callback behaviors.
/// </summary>
public class CallbackTests
{
    [Test]
    public async Task Callback_Is_Invoked_When_Method_Is_Called()
    {
        // Arrange
        var callbackInvoked = false;
        var mock = Mock.Of<ICalculator>();
        // Cast lambda to Action because Setup is dynamic
        mock.Setup.Log(Arg.Any<string>())
            .Callback((Action)(() => callbackInvoked = true));

        ICalculator calc = mock.Object;

        // Act
        calc.Log("test");

        // Assert
        await Assert.That(callbackInvoked).IsTrue();
    }

    [Test]
    public async Task Callback_With_Returns_Both_Execute()
    {
        // Arrange
        var callbackInvoked = false;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((Action)(() => callbackInvoked = true))
            .Then()
            .Returns(42);

        ICalculator calc = mock.Object;

        // Act — first call triggers callback (no return value behavior, returns default)
        var result1 = calc.Add(1, 2);
        await Assert.That(callbackInvoked).IsTrue();
        await Assert.That(result1).IsEqualTo(0); // callback returns null, so default(int)

        // Second call returns 42
        var result2 = calc.Add(1, 2);
        await Assert.That(result2).IsEqualTo(42);
    }

    [Test]
    public async Task Multiple_Callbacks_Via_Chaining()
    {
        // Arrange
        var callCount = 0;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Callback((Action)(() => callCount++))
            .Then()
            .Callback((Action)(() => callCount += 10));

        ICalculator calc = mock.Object;

        // Act
        calc.Log("first");  // callCount += 1 => 1
        calc.Log("second"); // callCount += 10 => 11

        // Assert
        await Assert.That(callCount).IsEqualTo(11);
    }

    [Test]
    public async Task Callback_Invoked_For_Each_Call()
    {
        // Arrange
        var callCount = 0;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Callback((Action)(() => callCount++));

        ICalculator calc = mock.Object;

        // Act — call multiple times; last behavior repeats
        calc.Log("a");
        calc.Log("b");
        calc.Log("c");

        // Assert — callback is the only behavior so it repeats
        await Assert.That(callCount).IsEqualTo(3);
    }

    [Test]
    public async Task Callback_On_Method_With_Return_Value()
    {
        // Arrange
        var lastArgs = "";
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Any<string>())
            .Callback((Action)(() => lastArgs = "called"))
            .Then()
            .Returns("hello");

        IGreeter greeter = mock.Object;

        // Act — first call: callback
        var result1 = greeter.Greet("Alice");
        await Assert.That(lastArgs).IsEqualTo("called");
        // Callback behavior returns null — null is correctly returned (not smart default)
        await Assert.That(result1).IsNull();

        // Second call: returns
        var result2 = greeter.Greet("Bob");
        await Assert.That(result2).IsEqualTo("hello");
    }

    [Test]
    public async Task Computed_Return_Via_Factory()
    {
        // Arrange
        var counter = 0;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns((Func<int>)(() => ++counter));

        ICalculator calc = mock.Object;

        // Act & Assert — factory is called each time
        await Assert.That(calc.Add(0, 0)).IsEqualTo(1);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(2);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(3);
    }
}
