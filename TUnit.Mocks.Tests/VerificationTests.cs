using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// US2 Integration Tests: Verification — verify method calls were made with expected arguments and counts.
/// </summary>
public class VerificationTests
{
    [Test]
    public async Task Verify_Once_Succeeds_When_Called_Once()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 2).Returns(3);

        // Act
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert
        mock.Verify.Add(1, 2).WasCalled(Times.Once);
        await Assert.That(true).IsTrue(); // test completes without exception
    }

    [Test]
    public async Task Verify_Fails_With_Descriptive_Message_When_Count_Wrong()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call once
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — verify expects exactly twice, should fail
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(2));
        });

        await Assert.That(exception.Message).Contains("Mock verification failed");
        await Assert.That(exception.ActualCount).IsEqualTo(1);
        await Assert.That(exception.ExpectedCall).Contains("Add");
    }

    [Test]
    public async Task Verify_Never_Succeeds_When_Not_Called()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — don't call anything

        // Assert
        mock.Verify.Add(1, 2).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_Never_Fails_When_Called()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasNeverCalled();
        });

        await Assert.That(exception.ActualCount).IsEqualTo(1);
    }

    [Test]
    public async Task Verify_AtLeast_Succeeds()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call 3 times
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Assert — at least 2 should pass
        mock.Verify.Add(1, 2).WasCalled(Times.AtLeast(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_AtMost_Succeeds()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call twice
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Assert — at most 3 should pass
        mock.Verify.Add(1, 2).WasCalled(Times.AtMost(3));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_AtMost_Fails_When_Exceeded()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call 3 times
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Assert — at most 2 should fail
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.AtMost(2));
        });

        await Assert.That(exception.ActualCount).IsEqualTo(3);
    }

    [Test]
    public async Task Verify_Between_Succeeds()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call twice
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        // Assert — between 1 and 3 should pass
        mock.Verify.Add(1, 2).WasCalled(Times.Between(1, 3));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_Between_Fails_When_Outside_Range()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call once
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — between 2 and 4 should fail (only 1 call)
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Between(2, 4));
        });

        await Assert.That(exception.ActualCount).IsEqualTo(1);
    }

    [Test]
    public async Task Verify_With_Exact_Args_Only_Matching_Calls_Count()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — call with different args
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(1, 2);

        // Assert — only calls with (1, 2) should count
        mock.Verify.Add(1, 2).WasCalled(Times.Exactly(2));
        mock.Verify.Add(3, 4).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_WasCalled_Shorthand_At_Least_Once()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — WasCalled() means at least once
        mock.Verify.Add(1, 2).WasCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_WasCalled_Shorthand_Fails_When_Not_Called()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Assert — WasCalled() should fail when not called
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
        });

        await Assert.That(exception.ActualCount).IsEqualTo(0);
    }

    [Test]
    public async Task Verify_Void_Method()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act
        ICalculator calc = mock.Object;
        calc.Log("hello");
        calc.Log("world");

        // Assert
        mock.Verify.Log("hello").WasCalled(Times.Once);
        mock.Verify.Log("world").WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verify_String_Method_On_Different_Interface()
    {
        // Arrange
        var mock = Mock.Of<IGreeter>();
        mock.Setup.Greet("Alice").Returns("Hello, Alice!");

        // Act
        IGreeter greeter = mock.Object;
        greeter.Greet("Alice");
        greeter.Greet("Bob");

        // Assert
        mock.Verify.Greet("Alice").WasCalled(Times.Once);
        mock.Verify.Greet("Bob").WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
