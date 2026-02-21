using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

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

    // ─── Strongly-Typed Callback Tests (Beyond Parity: US23) ────────────

    [Test]
    public async Task StronglyTyped_Returns_Computes_From_Arguments()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns((int a, int b) => a + b);

        ICalculator calc = mock.Object;

        await Assert.That(calc.Add(3, 7)).IsEqualTo(10);
        await Assert.That(calc.Add(100, 200)).IsEqualTo(300);
    }

    [Test]
    public async Task StronglyTyped_Callback_Receives_Arguments()
    {
        var mock = Mock.Of<ICalculator>();
        var capturedArgs = new List<(int a, int b)>();

        // Callback is a behavior — first call runs callback, second call runs Returns
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((int a, int b) => capturedArgs.Add((a, b)));

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);
        _ = calc.Add(10, 20);

        // Last behavior repeats — both calls run the callback
        await Assert.That(capturedArgs).HasCount().EqualTo(2);
        await Assert.That(capturedArgs[0]).IsEqualTo((1, 2));
        await Assert.That(capturedArgs[1]).IsEqualTo((10, 20));
    }

    [Test]
    public async Task StronglyTyped_Throws_With_Argument_Dependent_Exception()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Throws((int a, int b) => new ArgumentException($"Cannot add {a} and {b}"));

        ICalculator calc = mock.Object;

        var ex = Assert.Throws<ArgumentException>(() => { _ = calc.Add(3, 5); });
        await Assert.That(ex.Message).Contains("Cannot add 3 and 5");
    }

    [Test]
    public async Task StronglyTyped_Void_Callback()
    {
        var mock = Mock.Of<ICalculator>();
        string? capturedMessage = null;

        mock.Setup.Log(Arg.Any<string>())
            .Callback((string msg) => capturedMessage = msg);

        ICalculator calc = mock.Object;
        calc.Log("hello world");

        await Assert.That(capturedMessage).IsEqualTo("hello world");
    }

    [Test]
    public async Task StronglyTyped_Void_Throws()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Log(Arg.Any<string>())
            .Throws((string msg) => new InvalidOperationException($"Cannot log: {msg}"));

        ICalculator calc = mock.Object;

        var ex = Assert.Throws<InvalidOperationException>(() => calc.Log("test"));
        await Assert.That(ex.Message).Contains("Cannot log: test");
    }

    [Test]
    public async Task StronglyTyped_Returns_Single_Parameter()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet(Arg.Any<string>()).Returns((string name) => $"Hello, {name}!");

        IGreeter greeter = mock.Object;

        await Assert.That(greeter.Greet("World")).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task StronglyTyped_With_Then_Chain()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns((int a, int b) => a + b)
            .Then()
            .Returns(99);

        ICalculator calc = mock.Object;

        await Assert.That(calc.Add(3, 4)).IsEqualTo(7);
        await Assert.That(calc.Add(3, 4)).IsEqualTo(99);
    }

    [Test]
    public async Task StronglyTyped_Existing_Untyped_Returns_Still_Works()
    {
        // Verify that existing untyped API still compiles and works
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;

        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
    }

    [Test]
    public async Task StronglyTyped_Existing_Untyped_Callback_Still_Works()
    {
        var mock = Mock.Of<ICalculator>();
        var called = false;
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback(() => called = true)
            .Then()
            .Returns(0);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        await Assert.That(called).IsTrue();
    }
}
