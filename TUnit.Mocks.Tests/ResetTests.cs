using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// T073 Integration Tests: Mock reset — verify that Reset clears setups, call history,
/// and allows fresh re-configuration.
/// </summary>
public class ResetTests
{
    [Test]
    public async Task Reset_Clears_All_Setups()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(42);
        mock.Add(3, 4).Returns(99);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
        await Assert.That(calc.Add(3, 4)).IsEqualTo(99);

        // Act
        Mock.Reset(mock);

        // Assert — after reset, all setups are gone, returns default
        await Assert.That(calc.Add(1, 2)).IsEqualTo(0);
        await Assert.That(calc.Add(3, 4)).IsEqualTo(0);
    }

    [Test]
    public async Task Reset_Clears_Call_History()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        // Verify calls were recorded
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(3));

        // Act
        Mock.Reset(mock);

        // Assert — after reset, call history is cleared
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_Allows_Fresh_Setup()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 2).Returns(42);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);

        // Act — reset and reconfigure with different return value
        Mock.Reset(mock);
        mock.Add(1, 2).Returns(100);

        // Assert — new setup is in effect
        await Assert.That(calc.Add(1, 2)).IsEqualTo(100);
    }

    [Test]
    public async Task Reset_Allows_Fresh_Verification()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        mock.Add(1, 2).WasCalled(Times.Exactly(2));

        // Act
        Mock.Reset(mock);

        // Make one new call
        calc.Add(1, 2);

        // Assert — verification reflects only post-reset calls
        mock.Add(1, 2).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_On_Strict_Mock_Restores_Strict_Behavior()
    {
        // Arrange — strict mock with a configured method
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);

        // Act — reset clears all setups
        Mock.Reset(mock);

        // Assert — strict mock throws again for unconfigured calls
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(1, 2);
        });

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception.Message).Contains("Add");
    }

    [Test]
    public async Task Reset_Clears_String_Method_Setup()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Greet("Alice").Returns("Hello, Alice!");

        IGreeter greeter = mock.Object;
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("Hello, Alice!");

        // Act
        Mock.Reset(mock);

        // Assert — after reset, returns default (empty string for non-nullable string)
        var result = greeter.Greet("Alice");
        await Assert.That(result).IsNotEqualTo("Hello, Alice!");
    }

    [Test]
    public async Task Reset_Clears_Void_Method_Call_History()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Log("message1");
        calc.Log("message2");

        mock.Log(Arg.Any<string>()).WasCalled(Times.Exactly(2));

        // Act
        Mock.Reset(mock);

        // Assert — void method call history is cleared
        mock.Log(Arg.Any<string>()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_Followed_By_New_Setup_And_Verification()
    {
        // Arrange — full lifecycle: setup, use, reset, re-setup, re-use, re-verify
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 1).Returns(10);

        ICalculator calc = mock.Object;
        calc.Add(1, 1);
        mock.Add(1, 1).WasCalled(Times.Once);

        // Act — reset
        Mock.Reset(mock);

        // Re-setup with new values
        mock.Add(1, 1).Returns(20);

        // Re-use
        calc.Add(1, 1);
        calc.Add(1, 1);

        // Assert — new setup and history
        await Assert.That(calc.Add(1, 1)).IsEqualTo(20);
        mock.Add(1, 1).WasCalled(Times.Exactly(3)); // 2 from re-use + 1 from the assert line
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Multiple_Resets()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // First cycle
        mock.Add(1, 1).Returns(10);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(10);
        Mock.Reset(mock);

        // Second cycle
        mock.Add(1, 1).Returns(20);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(20);
        Mock.Reset(mock);

        // Third cycle
        mock.Add(1, 1).Returns(30);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(30);
        Mock.Reset(mock);

        // After final reset — returns default
        await Assert.That(calc.Add(1, 1)).IsEqualTo(0);
    }
}
