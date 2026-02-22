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
        mock.Setup.Add(1, 2).Returns(42);
        mock.Setup.Add(3, 4).Returns(99);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);
        await Assert.That(calc.Add(3, 4)).IsEqualTo(99);

        // Act
        mock.Reset();

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
        mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(3));

        // Act
        mock.Reset();

        // Assert — after reset, call history is cleared
        mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_Allows_Fresh_Setup()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 2).Returns(42);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(42);

        // Act — reset and reconfigure with different return value
        mock.Reset();
        mock.Setup.Add(1, 2).Returns(100);

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

        mock.Verify.Add(1, 2).WasCalled(Times.Exactly(2));

        // Act
        mock.Reset();

        // Make one new call
        calc.Add(1, 2);

        // Assert — verification reflects only post-reset calls
        mock.Verify.Add(1, 2).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_On_Strict_Mock_Restores_Strict_Behavior()
    {
        // Arrange — strict mock with a configured method
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Setup.Add(1, 2).Returns(3);

        ICalculator calc = mock.Object;
        await Assert.That(calc.Add(1, 2)).IsEqualTo(3);

        // Act — reset clears all setups
        mock.Reset();

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
        mock.Setup.Greet("Alice").Returns("Hello, Alice!");

        IGreeter greeter = mock.Object;
        await Assert.That(greeter.Greet("Alice")).IsEqualTo("Hello, Alice!");

        // Act
        mock.Reset();

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

        mock.Verify.Log(Arg.Any<string>()).WasCalled(Times.Exactly(2));

        // Act
        mock.Reset();

        // Assert — void method call history is cleared
        mock.Verify.Log(Arg.Any<string>()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Reset_Followed_By_New_Setup_And_Verification()
    {
        // Arrange — full lifecycle: setup, use, reset, re-setup, re-use, re-verify
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 1).Returns(10);

        ICalculator calc = mock.Object;
        calc.Add(1, 1);
        mock.Verify.Add(1, 1).WasCalled(Times.Once);

        // Act — reset
        mock.Reset();

        // Re-setup with new values
        mock.Setup.Add(1, 1).Returns(20);

        // Re-use
        calc.Add(1, 1);
        calc.Add(1, 1);

        // Assert — new setup and history
        await Assert.That(calc.Add(1, 1)).IsEqualTo(20);
        mock.Verify.Add(1, 1).WasCalled(Times.Exactly(3)); // 2 from re-use + 1 from the assert line
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Multiple_Resets()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // First cycle
        mock.Setup.Add(1, 1).Returns(10);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(10);
        mock.Reset();

        // Second cycle
        mock.Setup.Add(1, 1).Returns(20);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(20);
        mock.Reset();

        // Third cycle
        mock.Setup.Add(1, 1).Returns(30);
        await Assert.That(calc.Add(1, 1)).IsEqualTo(30);
        mock.Reset();

        // After final reset — returns default
        await Assert.That(calc.Add(1, 1)).IsEqualTo(0);
    }
}
