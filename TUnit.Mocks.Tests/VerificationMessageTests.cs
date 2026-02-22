using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for custom verification failure messages.
/// </summary>
public class VerificationMessageTests
{
    [Test]
    public async Task WasCalled_With_Custom_Message_Includes_It()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call once
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — verify expects exactly twice with custom message
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(2), "Expected retry logic to call twice");
        });

        await Assert.That(ex.Message).Contains("Expected retry logic to call twice");
        await Assert.That(ex.Message).Contains("Mock verification failed");
    }

    [Test]
    public async Task WasNeverCalled_With_Custom_Message_Includes_It()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasNeverCalled("Should not have called Add in read-only mode");
        });

        await Assert.That(ex.Message).Contains("Should not have called Add in read-only mode");
        await Assert.That(ex.Message).Contains("Mock verification failed");
    }

    [Test]
    public async Task WasCalled_Shorthand_With_Custom_Message_Includes_It()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Assert — not called at all
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled("Expected at least one Add call");
        });

        await Assert.That(ex.Message).Contains("Expected at least one Add call");
        await Assert.That(ex.Message).Contains("Mock verification failed");
    }

    [Test]
    public async Task WasCalled_Without_Message_Has_Standard_Output()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Once);
        });

        // Should NOT have a custom message line before "Mock verification failed."
        await Assert.That(ex.Message).StartsWith("Mock verification failed.");
    }

    [Test]
    public async Task WasCalled_With_Null_Message_Has_Standard_Output()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Once, null);
        });

        await Assert.That(ex.Message).StartsWith("Mock verification failed.");
    }

    [Test]
    public async Task WasCalled_With_Empty_Message_Has_Standard_Output()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Once, "");
        });

        await Assert.That(ex.Message).StartsWith("Mock verification failed.");
    }
}
