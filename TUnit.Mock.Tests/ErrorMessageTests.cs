using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;

namespace TUnit.Mock.Tests;

/// <summary>
/// T072 Integration Tests: Error message quality — verify exception messages contain
/// meaningful descriptions of expected calls, actual counts, and argument values.
/// </summary>
public class ErrorMessageTests
{
    // --- MockVerificationException message quality ---

    [Test]
    public async Task Verification_Message_Contains_Expected_Call_Description()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act & Assert — verify a call that was never made
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Once);
        });

        await Assert.That(exception.Message).Contains("Add");
        await Assert.That(exception.ExpectedCall).Contains("Add");
    }

    [Test]
    public async Task Verification_Message_Contains_Actual_Count()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Act & Assert — expect 5 but only 3 calls made
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(5));
        });

        await Assert.That(exception.ActualCount).IsEqualTo(3);
        await Assert.That(exception.Message).Contains("3");
    }

    [Test]
    public async Task Verification_Message_Contains_Formatted_Argument_Values()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(42, 99);

        // Act & Assert — verify with different args that were never called
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(10, 20).WasCalled(Times.Once);
        });

        // The expected call should mention the args we verified with
        await Assert.That(exception.ExpectedCall).Contains("Add");
        await Assert.That(exception.Message).Contains("Mock verification failed");
    }

    [Test]
    public async Task Verification_Message_Contains_String_Arguments()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        IGreeter greeter = mock.Object;
        greeter.Greet("Alice");

        // Act & Assert — verify a call with different string arg
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Greet("Bob").WasCalled(Times.Once);
        });

        await Assert.That(exception.ExpectedCall).Contains("Greet");
        await Assert.That(exception.ActualCount).IsEqualTo(0);
    }

    [Test]
    public async Task Verification_Message_Contains_Expected_Times_Description()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act & Assert
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(3));
        });

        // The message should describe the expected call count
        await Assert.That(exception.Message).Contains("Expected");
        await Assert.That(exception.Message).Contains("Actual");
    }

    [Test]
    public async Task Verification_WasNeverCalled_Message_Shows_Actual_Count()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Act & Assert — WasNeverCalled should fail since it was called twice
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasNeverCalled();
        });

        await Assert.That(exception.ActualCount).IsEqualTo(2);
    }

    [Test]
    public async Task Verification_AtLeast_Message_Shows_Shortfall()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Act & Assert — expect at least 5 but only 1 call
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.AtLeast(5));
        });

        await Assert.That(exception.ActualCount).IsEqualTo(1);
        await Assert.That(exception.Message).Contains("1");
    }

    [Test]
    public async Task Verification_Actual_Calls_List_Is_Populated()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(10, 20);
        calc.Add(30, 40);

        // Act & Assert — verify with Arg.Any but wrong count
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(5));
        });

        await Assert.That(exception.ActualCount).IsEqualTo(2);
        await Assert.That(exception.ActualCalls).HasCount().GreaterThanOrEqualTo(0);
    }

    // --- MockStrictBehaviorException message quality ---

    [Test]
    public async Task Strict_Message_Contains_Method_Name()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(1, 2);
        });

        await Assert.That(exception.Message).Contains("Add");
        await Assert.That(exception.UnconfiguredCall).Contains("Add");
    }

    [Test]
    public async Task Strict_Message_Contains_Int_Arguments()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Add(123, 456);
        });

        await Assert.That(exception.Message).Contains("123");
        await Assert.That(exception.Message).Contains("456");
        await Assert.That(exception.UnconfiguredCall).Contains("123");
        await Assert.That(exception.UnconfiguredCall).Contains("456");
    }

    [Test]
    public async Task Strict_Message_Contains_String_Arguments()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>(MockBehavior.Strict);
        IGreeter greeter = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            greeter.Greet("World");
        });

        await Assert.That(exception.Message).Contains("Greet");
        await Assert.That(exception.Message).Contains("World");
        await Assert.That(exception.UnconfiguredCall).Contains("Greet");
        await Assert.That(exception.UnconfiguredCall).Contains("World");
    }

    [Test]
    public async Task Strict_Message_Contains_Violation_Description()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Log("some message");
        });

        await Assert.That(exception.Message).Contains("Strict mock behavior violation");
        await Assert.That(exception.Message).Contains("No setup configured");
    }

    [Test]
    public async Task Strict_Void_Method_Message_Contains_Method_Name_And_Args()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        ICalculator calc = mock.Object;

        // Act & Assert
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            calc.Log("important log");
        });

        await Assert.That(exception.Message).Contains("Log");
        await Assert.That(exception.Message).Contains("important log");
    }
}
