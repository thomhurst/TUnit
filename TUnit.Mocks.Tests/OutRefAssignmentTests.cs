using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for out/ref parameter value assignment via typed and untyped APIs.
/// </summary>
public class OutRefAssignmentTests
{
    [Test]
    public async Task Out_Parameter_Can_Be_Set_Via_Typed_Api()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.TryGet("found")
            .Returns(true)
            .SetsOutValue("found-value");

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
        mock.TryGet("key").Returns(false);

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryGet("key", out var value);

        // Assert — no SetsOutParameter, so value stays default
        await Assert.That(success).IsFalse();
        await Assert.That(value).IsEqualTo(default(string)!);
    }

    [Test]
    public async Task Out_Parameter_Int_Can_Be_Set_Via_Typed_Api()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.TryParse(Arg.Any<string>())
            .Returns(true)
            .SetsOutResult(42);

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryParse("42", out var result);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Out_Parameter_With_Any_Matcher_Via_Typed_Api()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.TryGet(Arg.Any<string>())
            .Returns(true)
            .SetsOutValue("any-value");

        IDictionary dict = mock.Object;

        // Act — should work for any key
        dict.TryGet("a", out var v1);
        dict.TryGet("b", out var v2);

        // Assert
        await Assert.That(v1).IsEqualTo("any-value");
        await Assert.That(v2).IsEqualTo("any-value");
    }

    [Test]
    public async Task Ref_Parameter_Can_Be_Set_Via_Typed_Api()
    {
        // Arrange
        var mock = Mock.Of<IDictionary>();
        mock.Swap(Arg.Any<int>())
            .SetsRefValue(99);

        IDictionary dict = mock.Object;

        // Act
        int val = 42;
        dict.Swap(ref val);

        // Assert
        await Assert.That(val).IsEqualTo(99);
    }

    [Test]
    public async Task Untyped_SetsOutParameter_Still_Works()
    {
        // Arrange — backward compatibility with untyped API
        var mock = Mock.Of<IDictionary>();
        mock.TryGet("key")
            .Returns(true)
            .SetsOutParameter(1, "untyped-value");

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryGet("key", out var value);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(value).IsEqualTo("untyped-value");
    }

    [Test]
    public async Task Typed_And_Chaining_Works()
    {
        // Arrange — chain typed out/ref setter with Returns
        var mock = Mock.Of<IDictionary>();
        mock.TryGet(Arg.Any<string>())
            .SetsOutValue("chained")
            .Returns(true);

        IDictionary dict = mock.Object;

        // Act
        var success = dict.TryGet("x", out var value);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(value).IsEqualTo("chained");
    }
}
