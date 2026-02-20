using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Interfaces with out/ref parameters for testing parameter direction support.
/// </summary>
public interface IDictionary
{
    bool TryGet(string key, out string value);
    void Swap(ref int value);
    bool TryParse(string input, out int result);
}

/// <summary>
/// US7 Integration Tests: out/ref parameter support in mock generation.
/// </summary>
public class OutRefTests
{
    [Test]
    public async Task Out_Parameter_Method_Can_Be_Called()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act - should not throw, out param gets default
        IDictionary dict = mock.Object;
        var found = dict.TryGet("key", out var value);

        // Assert - unconfigured returns default
        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task Out_Parameter_Method_Verify_Call_Was_Made()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act
        IDictionary dict = mock.Object;
        dict.TryGet("hello", out _);

        // Assert - verify with the input arg only (out param excluded from matchers)
        mock.Verify.TryGet("hello").WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Out_Parameter_Method_Setup_Return_Value()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryGet("found").Returns(true);

        // Act
        IDictionary dict = mock.Object;
        var result = dict.TryGet("found", out _);

        // Assert - the return value is configured
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Out_Parameter_Method_With_Any_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryGet(Arg.Any<string>()).Returns(true);

        // Act
        IDictionary dict = mock.Object;
        var r1 = dict.TryGet("a", out _);
        var r2 = dict.TryGet("b", out _);

        // Assert
        await Assert.That(r1).IsTrue();
        await Assert.That(r2).IsTrue();
    }

    [Test]
    public async Task Out_Parameter_Int_Method_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act
        IDictionary dict = mock.Object;
        var success = dict.TryParse("42", out var result);

        // Assert - unconfigured returns default
        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task Out_Parameter_Method_Verify_Never_Called()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Assert - never called
        mock.Verify.TryGet("key").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Ref_Parameter_Method_Can_Be_Called()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act - ref param is passed in and out, but mock just records
        IDictionary dict = mock.Object;
        int val = 42;
        dict.Swap(ref val);

        // Assert - in loose mode, void method doesn't throw
        // val is unchanged since the mock doesn't modify it
        await Assert.That(val).IsEqualTo(42);
    }

    [Test]
    public async Task Ref_Parameter_Method_Verify_Call()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act
        IDictionary dict = mock.Object;
        int val = 10;
        dict.Swap(ref val);

        // Assert - ref params ARE included in matchers
        mock.Verify.Swap(10).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Multiple_Out_Method_Calls_Tracked()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();

        // Act
        IDictionary dict = mock.Object;
        dict.TryGet("a", out _);
        dict.TryGet("b", out _);
        dict.TryGet("a", out _);

        // Assert
        mock.Verify.TryGet("a").WasCalled(Times.Exactly(2));
        mock.Verify.TryGet("b").WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
