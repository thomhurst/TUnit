#nullable enable
using TUnit.Assertions.Exceptions;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for Assert.NotNull() and Assert.Null() methods.
/// These methods properly update null-state flow analysis via [NotNull] attribute.
/// </summary>
public class AssertNotNullTests
{
    [Test]
    public async Task NotNull_WithNonNullReferenceType_DoesNotThrow()
    {
        string? value = "test";

        Assert.NotNull(value);

        // After NotNull, the compiler should know value is non-null
        // This should compile without warnings
        var length = value.Length;

        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task NotNull_WithNullReferenceType_Throws()
    {
        string? value = null;

        var exception = Assert.Throws<AssertionException>(() => Assert.NotNull(value));

        await Assert.That(exception.Message).Contains("to not be null");
    }

    [Test]
    public async Task NotNull_WithNonNullableValueType_DoesNotThrow()
    {
        int? value = 42;

        Assert.NotNull(value);

        // After NotNull, the compiler should know value has a value
        var intValue = value.Value;

        await Assert.That(intValue).IsEqualTo(42);
    }

    [Test]
    public async Task NotNull_WithNullableValueType_Throws()
    {
        int? value = null;

        var exception = Assert.Throws<AssertionException>(() => Assert.NotNull(value));

        await Assert.That(exception.Message).Contains("to not be null");
    }

    [Test]
    public void Null_WithNullReferenceType_DoesNotThrow()
    {
        string? value = null;

        Assert.Null(value);

        // Test passes if no exception thrown
    }

    [Test]
    public async Task Null_WithNonNullReferenceType_Throws()
    {
        string? value = "test";

        var exception = Assert.Throws<AssertionException>(() => Assert.Null(value));

        await Assert.That(exception.Message).Contains("to be null");
    }

    [Test]
    public void Null_WithNullValueType_DoesNotThrow()
    {
        int? value = null;

        Assert.Null(value);

        // Test passes if no exception thrown
    }

    [Test]
    public async Task Null_WithNonNullValueType_Throws()
    {
        int? value = 42;

        var exception = Assert.Throws<AssertionException>(() => Assert.Null(value));

        await Assert.That(exception.Message).Contains("to be null");
    }

    [Test]
    public async Task NotNull_CapturesExpressionInMessage()
    {
        string? myVariable = null;

        var exception = Assert.Throws<AssertionException>(() => Assert.NotNull(myVariable));

        await Assert.That(exception.Message).Contains("myVariable");
    }

    [Test]
    public async Task Null_CapturesExpressionInMessage()
    {
        string myVariable = "not null";

        var exception = Assert.Throws<AssertionException>(() => Assert.Null(myVariable));

        await Assert.That(exception.Message).Contains("myVariable");
    }

    [Test]
    public async Task NotNull_AllowsChainingWithOtherAssertions()
    {
        string? value = "test";

        Assert.NotNull(value);

        // Can use the value directly without null-forgiving operator
        await Assert.That(value.ToUpper()).IsEqualTo("TEST");
        await Assert.That(value.Length).IsEqualTo(4);
    }

    [Test]
    public async Task NotNull_WithComplexObject_UpdatesNullState()
    {
        var obj = new TestClass { Name = "test" };
        TestClass? nullableObj = obj;

        Assert.NotNull(nullableObj);

        // Should be able to access properties without warnings
        var name = nullableObj.Name;
        await Assert.That(name).IsEqualTo("test");
    }

    private class TestClass
    {
        public string? Name { get; set; }
    }
}
