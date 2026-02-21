using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Tests for typed callbacks with arguments, computed returns with args, and computed throws.
/// </summary>
public class TypedCallbackTests
{
    [Test]
    public async Task Callback_With_Args_Receives_Arguments()
    {
        // Arrange
        object?[]? capturedArgs = null;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((Action<object?[]>)(args => capturedArgs = args));

        ICalculator calc = mock.Object;

        // Act
        calc.Add(3, 7);

        // Assert
        await Assert.That(capturedArgs).IsNotNull();
        await Assert.That(capturedArgs!.Length).IsEqualTo(2);
        await Assert.That(capturedArgs[0]).IsEqualTo(3);
        await Assert.That(capturedArgs[1]).IsEqualTo(7);
    }

    [Test]
    public async Task Callback_With_Args_On_Void_Method()
    {
        // Arrange
        object?[]? capturedArgs = null;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Callback((Action<object?[]>)(args => capturedArgs = args));

        ICalculator calc = mock.Object;

        // Act
        calc.Log("hello");

        // Assert
        await Assert.That(capturedArgs).IsNotNull();
        await Assert.That(capturedArgs!.Length).IsEqualTo(1);
        await Assert.That(capturedArgs[0]).IsEqualTo("hello");
    }

    [Test]
    public async Task Returns_With_Args_Computes_From_Arguments()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns((Func<object?[], int>)(args => (int)args[0]! + (int)args[1]!));

        ICalculator calc = mock.Object;

        // Act & Assert
        await Assert.That(calc.Add(3, 7)).IsEqualTo(10);
        await Assert.That(calc.Add(100, 200)).IsEqualTo(300);
        await Assert.That(calc.Add(-5, 5)).IsEqualTo(0);
    }

    [Test]
    public async Task Returns_With_Args_String_Concatenation()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Any<string>())
            .Returns((Func<object?[], string>)(args => $"Hello, {args[0]}!"));

        IGreeter greeter = mock.Object;

        // Act & Assert
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("Hello, Alice!");
        await Assert.That(greeter.Greet("Bob")).IsEqualTo("Hello, Bob!");
    }

    [Test]
    public async Task Computed_Throw_With_Args()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Throws((Func<object?[], Exception>)(args =>
                new ArgumentException($"Bad args: {args[0]}, {args[1]}")));

        ICalculator calc = mock.Object;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => calc.Add(1, 2));
        await Assert.That(ex.Message).Contains("Bad args: 1, 2");
    }

    [Test]
    public async Task Computed_Throw_On_Void_Method()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Throws((Func<object?[], Exception>)(args =>
                new InvalidOperationException($"Cannot log: {args[0]}")));

        ICalculator calc = mock.Object;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => calc.Log("secret"));
        await Assert.That(ex.Message).Contains("Cannot log: secret");
    }

    [Test]
    public async Task Callback_With_Args_Then_Returns()
    {
        // Arrange
        object?[]? capturedArgs = null;
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((Action<object?[]>)(args => capturedArgs = args))
            .Then()
            .Returns(42);

        ICalculator calc = mock.Object;

        // Act - first call triggers callback
        var result1 = calc.Add(5, 10);
        await Assert.That(capturedArgs).IsNotNull();
        await Assert.That(capturedArgs![0]).IsEqualTo(5);

        // Second call returns fixed value
        var result2 = calc.Add(1, 1);
        await Assert.That(result2).IsEqualTo(42);
    }

    [Test]
    public async Task Returns_With_Args_Repeats_On_Subsequent_Calls()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns((Func<object?[], int>)(args => (int)args[0]! * (int)args[1]!));

        ICalculator calc = mock.Object;

        // Act & Assert — last behavior repeats
        await Assert.That(calc.Add(3, 4)).IsEqualTo(12);
        await Assert.That(calc.Add(5, 6)).IsEqualTo(30);
        await Assert.That(calc.Add(7, 8)).IsEqualTo(56);
    }
}
