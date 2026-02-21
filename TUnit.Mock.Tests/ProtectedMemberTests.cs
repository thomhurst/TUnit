using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Abstract class with protected virtual members for testing.
/// </summary>
public abstract class ProtectedServiceBase
{
    public abstract string GetName();

    protected virtual int ComputeValue(int input)
    {
        return input * 2;
    }

    protected abstract string FormatResult(int value);

    public string ProcessAndFormat(int input)
    {
        var value = ComputeValue(input);
        return FormatResult(value);
    }
}

/// <summary>
/// US17 Tests: Protected member mocking.
/// </summary>
public class ProtectedMemberTests
{
    [Test]
    public async Task Protected_Virtual_Method_Calls_Base_When_Not_Configured()
    {
        // Arrange
        var mock = Mock.OfPartial<ProtectedServiceBase>();
        mock.Setup.GetName().Returns("Test");
        mock.Setup.FormatResult(Arg.Any<int>()).Returns("formatted");

        // Act — ComputeValue is protected virtual, not configured → calls base (input * 2)
        var result = mock.Object.ProcessAndFormat(5);

        // Assert — FormatResult receives 10 (5 * 2 from base ComputeValue)
        mock.Verify!.FormatResult(Arg.Is(10)).WasCalled();
        await Assert.That(result).IsEqualTo("formatted");
    }

    [Test]
    public async Task Protected_Virtual_Method_Can_Be_Configured()
    {
        // Arrange
        var mock = Mock.OfPartial<ProtectedServiceBase>();
        mock.Setup.GetName().Returns("Test");
        mock.Setup.ComputeValue(Arg.Any<int>()).Returns(42);
        mock.Setup.FormatResult(Arg.Any<int>()).Returns("configured");

        // Act — ComputeValue is configured to return 42
        var result = mock.Object.ProcessAndFormat(5);

        // Assert — FormatResult receives 42 (from configured ComputeValue)
        mock.Verify!.FormatResult(Arg.Is(42)).WasCalled();
        await Assert.That(result).IsEqualTo("configured");
    }

    [Test]
    public async Task Protected_Abstract_Method_Can_Be_Configured()
    {
        // Arrange
        var mock = Mock.OfPartial<ProtectedServiceBase>();
        mock.Setup.GetName().Returns("Test");
        mock.Setup.FormatResult(Arg.Any<int>()).Returns("custom format");

        // Act
        var result = mock.Object.ProcessAndFormat(3);

        // Assert
        await Assert.That(result).IsEqualTo("custom format");
    }

    [Test]
    public async Task Protected_Method_Calls_Are_Recorded_In_Invocations()
    {
        // Arrange
        var mock = Mock.OfPartial<ProtectedServiceBase>();
        mock.Setup.GetName().Returns("Test");
        mock.Setup.FormatResult(Arg.Any<int>()).Returns("result");

        // Act
        mock.Object.ProcessAndFormat(7);

        // Assert — both ComputeValue (base call) and FormatResult are recorded
        await Assert.That(mock.Invocations).Count().IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Protected_Method_Can_Be_Verified()
    {
        // Arrange
        var mock = Mock.OfPartial<ProtectedServiceBase>();
        mock.Setup.GetName().Returns("Test");
        mock.Setup.FormatResult(Arg.Any<int>()).Returns("result");

        // Act
        mock.Object.ProcessAndFormat(5);

        // Assert — verify protected methods were called
        mock.Verify!.ComputeValue(Arg.Is(5)).WasCalled();
        mock.Verify!.FormatResult(Arg.Any<int>()).WasCalled();
    }
}
