using TUnit.Mock;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Tests;

/// <summary>
/// US6 Integration Tests: Strict mode behavior — unconfigured calls throw MockStrictBehaviorException.
/// </summary>
public class StrictModeTests
{
    [Test]
    public async Task Strict_Unconfigured_Void_Method_Throws()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert — unconfigured void method should throw
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Log("test message");
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Strict_Unconfigured_Return_Method_Throws()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert — unconfigured return method should throw
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(1, 2);
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Strict_Configured_Method_Works_Normally()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Setup.Add(2, 3).Returns(5);

        // Act
        ICalculator calc = mock.Object;
        var result = calc.Add(2, 3);

        // Assert — configured method returns the setup value
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task Strict_Configured_Void_Method_Does_Not_Throw()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Setup.Log("expected message");

        // Act & Assert — configured void method should not throw
        ICalculator calc = mock.Object;
        calc.Log("expected message");
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Strict);
    }

    [Test]
    public async Task Strict_Error_Message_Contains_Method_Name()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(10, 20);
        });

        await Assert.That(exception.Message).Contains("Add");
    }

    [Test]
    public async Task Strict_Error_Message_Contains_Arguments()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(42, 99);
        });

        await Assert.That(exception.Message).Contains("42");
        await Assert.That(exception.Message).Contains("99");
    }

    [Test]
    public async Task Strict_Error_Message_Contains_String_Arguments()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>(MockBehavior.Strict);
        IGreeter greeter = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            greeter.Greet("Alice");
        });

        await Assert.That(exception.Message).Contains("Greet");
        await Assert.That(exception.Message).Contains("Alice");
    }

    [Test]
    public async Task Strict_Mixed_Some_Configured_Some_Not()
    {
        // Arrange — configure Add but not Log or GetName
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Setup.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;

        // Act — configured method works
        var result = calc.Add(1, 2);
        await Assert.That(result).IsEqualTo(3);

        // Act & Assert — unconfigured void method throws
        var voidException = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Log("hello");
        });
        await Assert.That(voidException.Message).Contains("Log");

        // Act & Assert — unconfigured return method throws
        var returnException = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.GetName();
        });
        await Assert.That(returnException.Message).Contains("GetName");
    }

    [Test]
    public async Task Strict_Configured_Args_Mismatch_Throws()
    {
        // Arrange — configure Add with specific args
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Setup.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;

        // Act — matching args works
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);

        // Act & Assert — different args have no matching setup, should throw
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(10, 20);
        });
        await Assert.That(exception.Message).Contains("Add");
    }

    [Test]
    public async Task Strict_UnconfiguredCall_Property_Contains_Call_Info()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(7, 8);
        });

        await Assert.That(exception.UnconfiguredCall).Contains("Add");
        await Assert.That(exception.UnconfiguredCall).Contains("7");
        await Assert.That(exception.UnconfiguredCall).Contains("8");
    }

    [Test]
    public async Task Loose_Is_Default_Behavior()
    {
        // Arrange — no behavior specified, should be Loose
        var mock = Mock.Of<ICalculator>();

        // Act — unconfigured methods should NOT throw
        ICalculator calc = mock.Object;
        var result = calc.Add(1, 2);
        calc.Log("no throw");

        // Assert
        await Assert.That(result).IsEqualTo(0);
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Loose);
    }
}
