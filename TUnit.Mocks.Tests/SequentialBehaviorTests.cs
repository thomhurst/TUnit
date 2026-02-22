using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Integration tests for sequential behavior chaining (.Then(), .ReturnsSequentially()).
/// </summary>
public class SequentialBehaviorTests
{
    [Test]
    public async Task Throws_Then_Returns_First_Call_Throws_Second_Returns()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Throws<InvalidOperationException>()
            .Then()
            .Returns(5);

        ICalculator calc = mock.Object;

        // Act & Assert — first call throws
        var exception = Assert.Throws<InvalidOperationException>(() => calc.Add(1, 2));
        await Assert.That(exception).IsNotNull();

        // Second call returns 5
        var result = calc.Add(1, 2);
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task ReturnsSequentially_Returns_Values_In_Order()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .ReturnsSequentially(1, 2, 3);

        ICalculator calc = mock.Object;

        // Act & Assert — values returned in order
        await Assert.That(calc.Add(0, 0)).IsEqualTo(1);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(2);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(3);
    }

    [Test]
    public async Task ReturnsSequentially_Last_Value_Repeats()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .ReturnsSequentially(10, 20);

        ICalculator calc = mock.Object;

        // Act & Assert — after all values consumed, last repeats
        await Assert.That(calc.Add(0, 0)).IsEqualTo(10);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(20);
        await Assert.That(calc.Add(0, 0)).IsEqualTo(20); // repeats
        await Assert.That(calc.Add(0, 0)).IsEqualTo(20); // still repeats
    }

    [Test]
    public async Task Void_Method_Callback_Then_Throws()
    {
        // Arrange
        var callbackInvoked = false;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Callback(() => callbackInvoked = true)
            .Then()
            .Throws<InvalidOperationException>();

        ICalculator calc = mock.Object;

        // Act — first call invokes callback
        calc.Log("first");
        await Assert.That(callbackInvoked).IsTrue();

        // Second call throws
        var exception = Assert.Throws<InvalidOperationException>(() => calc.Log("second"));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Returns_Then_Throws_Sequence()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1)
            .Returns(42)
            .Then()
            .Throws<InvalidOperationException>();

        ICalculator calc = mock.Object;

        // Act & Assert — first call returns 42
        await Assert.That(calc.Add(1, 1)).IsEqualTo(42);

        // Second call throws
        var exception = Assert.Throws<InvalidOperationException>(() => calc.Add(1, 1));
        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Chained_Returns_With_Then()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Any<string>())
            .Returns("first")
            .Then()
            .Returns("second")
            .Then()
            .Returns("third");

        IGreeter greeter = mock.Object;

        // Act & Assert — each call gets different return value
        await Assert.That(greeter.Greet("a")).IsEqualTo("first");
        await Assert.That(greeter.Greet("b")).IsEqualTo("second");
        await Assert.That(greeter.Greet("c")).IsEqualTo("third");
        // Fourth call repeats last
        await Assert.That(greeter.Greet("d")).IsEqualTo("third");
    }
}
