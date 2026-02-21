using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Tests;

/// <summary>
/// Test class used for wrap mock tests.
/// </summary>
public class RealCalculator
{
    private readonly int _baseValue;

    public RealCalculator() : this(0) { }

    public RealCalculator(int baseValue)
    {
        _baseValue = baseValue;
    }

    public virtual int Add(int a, int b) => a + b + _baseValue;
    public virtual string Describe(string operation) => $"Calculator({_baseValue}): {operation}";
    public virtual void Log(string message) { /* side-effect */ }
    public virtual int Multiply(int a, int b) => a * b;
}

/// <summary>
/// US3 Integration Tests: Wrap a real object — unconfigured calls go to the real instance.
/// </summary>
public class WrapRealObjectTests
{
    [Test]
    public async Task Wrap_Creates_Mock_Around_Real_Instance()
    {
        // Arrange
        var real = new RealCalculator(10);

        // Act
        var mock = Mock.Wrap(real);

        // Assert
        await Assert.That(mock).IsNotNull();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Unconfigured_Call_Delegates_To_Wrapped_Instance()
    {
        // Arrange
        var real = new RealCalculator(10);
        var mock = Mock.Wrap(real);

        // Act — no setup, should delegate to real instance
        var result = mock.Object.Add(3, 4);

        // Assert — real instance adds baseValue (10): 3 + 4 + 10 = 17
        await Assert.That(result).IsEqualTo(17);
    }

    [Test]
    public async Task Configured_Call_Returns_Mock_Value_Instead_Of_Real()
    {
        // Arrange
        var real = new RealCalculator(10);
        var mock = Mock.Wrap(real);
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(99);

        // Act
        var result = mock.Object.Add(3, 4);

        // Assert — configured value overrides real instance
        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    public async Task Multiple_Methods_Mixed_Setup()
    {
        // Arrange
        var real = new RealCalculator(5);
        var mock = Mock.Wrap(real);
        mock.Setup.Add(1, 2).Returns(100);
        // Don't setup Multiply — let it go to wrapped instance

        // Act
        var addResult = mock.Object.Add(1, 2);
        var mulResult = mock.Object.Multiply(3, 4);

        // Assert
        await Assert.That(addResult).IsEqualTo(100);  // mocked
        await Assert.That(mulResult).IsEqualTo(12);    // real: 3 * 4
    }

    [Test]
    public async Task String_Method_Delegates_To_Real_Instance()
    {
        // Arrange
        var real = new RealCalculator(42);
        var mock = Mock.Wrap(real);

        // Act — not configured, should use real instance
        var result = mock.Object.Describe("test");

        // Assert
        await Assert.That(result).IsEqualTo("Calculator(42): test");
    }

    [Test]
    public async Task String_Method_Can_Be_Overridden()
    {
        // Arrange
        var real = new RealCalculator(42);
        var mock = Mock.Wrap(real);
        mock.Setup.Describe(Arg.Any<string>()).Returns("mocked description");

        // Act
        var result = mock.Object.Describe("test");

        // Assert
        await Assert.That(result).IsEqualTo("mocked description");
    }

    [Test]
    public void Void_Method_Delegates_To_Real_Instance()
    {
        // Arrange
        var real = new RealCalculator();
        var mock = Mock.Wrap(real);

        // Act — void method, no setup, calls through to real instance
        mock.Object.Log("test message");

        // Assert — call was recorded for verification
        mock.Verify.Log(Arg.Any<string>()).WasCalled(Times.Once);
    }

    [Test]
    public void Verify_Records_Calls_Even_When_Delegating()
    {
        // Arrange
        var real = new RealCalculator(10);
        var mock = Mock.Wrap(real);

        // Act — unconfigured, delegates to real
        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);
        mock.Object.Multiply(5, 6);

        // Assert — calls are still tracked
        mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(2));
        mock.Verify.Multiply(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Wrap_With_Specific_Behavior()
    {
        // Arrange
        var real = new RealCalculator(10);
        var mock = Mock.Wrap(MockBehavior.Loose, real);

        // Act — loose mode, unconfigured call goes to wrapped instance
        var result = mock.Object.Add(1, 2);

        // Assert
        await Assert.That(result).IsEqualTo(13); // 1 + 2 + 10
    }

    [Test]
    public async Task Wrap_Strict_Mode_Unconfigured_Virtual_Calls_Wrapped()
    {
        // Arrange — strict mode, but virtual methods should still call wrapped instance when unconfigured
        var real = new RealCalculator(10);
        var mock = Mock.Wrap(MockBehavior.Strict, real);

        // Act — virtual method with no setup delegates to real
        var result = mock.Object.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(15); // 2 + 3 + 10
    }

    [Test]
    public async Task Wrap_Returns_Configured_Value()
    {
        // Arrange
        var real = new RealCalculator(100);
        var mock = Mock.Wrap(real);
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(0);

        // Act
        var result = mock.Object.Add(5, 7);

        // Assert — returns configured value, not real (5 + 7 + 100)
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public void Wrap_Throws_Works()
    {
        // Arrange
        var real = new RealCalculator();
        var mock = Mock.Wrap(real);
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>())
            .Throws(new InvalidOperationException("mock error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => mock.Object.Add(1, 2));
    }
}
