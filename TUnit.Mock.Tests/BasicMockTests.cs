using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Test interfaces used across mock tests.
/// </summary>
public interface ICalculator
{
    int Add(int a, int b);
    string GetName();
    void Log(string message);
}

public interface IGreeter
{
    string Greet(string name);
}

/// <summary>
/// US1 Integration Tests: Create a mock and configure return values.
/// </summary>
public class BasicMockTests
{
    [Test]
    public async Task Mock_Of_Creates_Mock_Instance()
    {
        // Arrange & Act
        var mock = Mock.Of<ICalculator>();

        // Assert
        await Assert.That(mock).IsNotNull();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Setup_Returns_Configures_Return_Value()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(2, 3).Returns(5);

        // Act
        ICalculator calc = mock.Object;
        var result = calc.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task Setup_Returns_With_String()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet("Alice").Returns("Hello, Alice!");

        // Act
        IGreeter greeter = mock.Object;
        var result = greeter.Greet("Alice");

        // Assert
        await Assert.That(result).IsEqualTo("Hello, Alice!");
    }

    [Test]
    public async Task Unconfigured_Method_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        ICalculator calc = mock.Object;
        var result = calc.Add(1, 2);

        // Assert — default for int is 0
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Conversion_To_Interface()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — .Object access
        ICalculator calc = mock.Object;

        // Assert
        await Assert.That(calc).IsNotNull();
    }

    [Test]
    public async Task Multiple_Setups_Last_Wins()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1).Returns(10);
        mock.Setup.Add(1, 1).Returns(20); // Last setup wins

        // Act
        ICalculator calc = mock.Object;
        var result = calc.Add(1, 1);

        // Assert
        await Assert.That(result).IsEqualTo(20);
    }

    [Test]
    public async Task Different_Args_Different_Returns()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 2).Returns(3);
        mock.Setup.Add(10, 20).Returns(30);

        // Act
        ICalculator calc = mock.Object;

        // Assert
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);
        await Assert.That(calc.Add(10, 20)).IsEqualTo(30);
        await Assert.That(calc.Add(99, 99)).IsEqualTo(0); // No setup, returns default
    }

    [Test]
    public void Void_Method_Does_Not_Throw_In_Loose_Mode()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act & Assert — should not throw
        ICalculator calc = mock.Object;
        calc.Log("test message");
    }

    [Test]
    public async Task String_Method_Unconfigured_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        ICalculator calc = mock.Object;
        var result = calc.GetName();

        // Assert — smart default for non-nullable string is ""
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task Reset_Clears_Setups()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1).Returns(42);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 1)).IsEqualTo(42);

        // Act
        mock.Reset();

        // Assert — after reset, returns default
        await Assert.That(calc.Add(1, 1)).IsEqualTo(0);
    }
}
