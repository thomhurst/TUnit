using TUnit.Mock;
using TUnit.Mock.Arguments;

namespace TUnit.Mock.Tests;

/// <summary>
/// Tests for out/ref parameter value assignment via SetsOutParameter.
/// </summary>
public class OutRefAssignmentTests
{
    [Test]
    public async Task Out_Parameter_Can_Be_Set_Via_Setup()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryGet("found")
            .Returns(true)
            .SetsOutParameter(1, "found-value");

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryGet("found", out var value);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(value).IsEqualTo("found-value");
    }

    [Test]
    public async Task Out_Parameter_Stays_Default_Without_Assignment()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryGet("key").Returns(false);

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryGet("key", out var value);

        // Assert — no SetsOutParameter, so value stays default
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(string)!);
    }

    [Test]
    public async Task Out_Parameter_Int_Can_Be_Set()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryParse(Arg.Any<string>())
            .Returns(true)
            .SetsOutParameter(1, 42);

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryParse("42", out var result);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Out_Parameter_With_Any_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.TryGet(Arg.Any<string>())
            .Returns(true)
            .SetsOutParameter(1, "any-value");

        IDictionary dict = mock.Object;

        // Act — should work for any key
        dict.TryGet("a", out var v1);
        dict.TryGet("b", out var v2);

        // Assert
        await Assert.That(v1).IsEqualTo("any-value");
        await Assert.That(v2).IsEqualTo("any-value");
    }

    [Test]
    public async Task Ref_Parameter_Can_Be_Set_Via_Setup()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Setup.Swap(Arg.Any<int>())
            .SetsOutParameter(0, 99);

        IDictionary dict = mock.Object;

        // Act
        int val = 42;
        dict.Swap(ref val);

        // Assert
        await Assert.That(val).IsEqualTo(99);
    }
}
