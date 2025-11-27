using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests;

public class StringLengthAssertionTests
{
    [Test]
    public async Task Length_IsEqualTo_Passes_When_Length_Matches()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsEqualTo(5);
    }

    [Test]
    public async Task Length_IsEqualTo_Fails_When_Length_DoesNotMatch()
    {
        var str = "Hello";
        await Assert.That(async () => await Assert.That(str).Length().IsEqualTo(10))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("10");
    }

    [Test]
    public async Task Length_IsGreaterThan_Passes_When_Length_IsGreater()
    {
        var str = "Hello, World!";
        await Assert.That(str).Length().IsGreaterThan(5);
    }

    [Test]
    public async Task Length_IsGreaterThan_Fails_When_Length_IsNotGreater()
    {
        var str = "Hi";
        await Assert.That(async () => await Assert.That(str).Length().IsGreaterThan(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("greater than")
            .And.HasMessageContaining("5");
    }

    [Test]
    public async Task Length_IsLessThan_Passes_When_Length_IsLess()
    {
        var str = "Hi";
        await Assert.That(str).Length().IsLessThan(10);
    }

    [Test]
    public async Task Length_IsLessThan_Fails_When_Length_IsNotLess()
    {
        var str = "Hello, World!";
        await Assert.That(async () => await Assert.That(str).Length().IsLessThan(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("less than")
            .And.HasMessageContaining("5");
    }

    [Test]
    public async Task Length_IsGreaterThanOrEqualTo_Passes_When_Equal()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task Length_IsGreaterThanOrEqualTo_Passes_When_Greater()
    {
        var str = "Hello, World!";
        await Assert.That(str).Length().IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task Length_IsGreaterThanOrEqualTo_Fails_When_Less()
    {
        var str = "Hi";
        await Assert.That(async () => await Assert.That(str).Length().IsGreaterThanOrEqualTo(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("greater than or equal to")
            .And.HasMessageContaining("5");
    }

    [Test]
    public async Task Length_IsLessThanOrEqualTo_Passes_When_Equal()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task Length_IsLessThanOrEqualTo_Passes_When_Less()
    {
        var str = "Hi";
        await Assert.That(str).Length().IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task Length_IsLessThanOrEqualTo_Fails_When_Greater()
    {
        var str = "Hello, World!";
        await Assert.That(async () => await Assert.That(str).Length().IsLessThanOrEqualTo(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("less than or equal to")
            .And.HasMessageContaining("5");
    }

    [Test]
    public async Task Length_IsBetween_Passes_When_InRange()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsBetween(1, 10);
    }

    [Test]
    public async Task Length_IsBetween_Fails_When_OutOfRange()
    {
        var str = "Hello, World!";
        await Assert.That(async () => await Assert.That(str).Length().IsBetween(1, 5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("between");
    }

    [Test]
    public async Task Length_IsPositive_Passes_For_NonEmptyString()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsPositive();
    }

    [Test]
    public async Task Length_IsGreaterThanOrEqualTo_Zero_Passes_Always()
    {
        var str = "";
        await Assert.That(str).Length().IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Length_IsZero_Passes_For_EmptyString()
    {
        var str = "";
        await Assert.That(str).Length().IsZero();
    }

    [Test]
    public async Task Length_IsNotZero_Passes_For_NonEmptyString()
    {
        var str = "Hello";
        await Assert.That(str).Length().IsNotZero();
    }

    [Test]
    public async Task Length_WithNullString_Returns_Zero()
    {
        string? str = null;
        await Assert.That(str).Length().IsZero();
    }

    [Test]
    public async Task Length_Chained_With_And()
    {
        var str = "Hello";
        await Assert.That(str)
            .Length().IsGreaterThan(3)
            .And.IsLessThan(10);
    }
}
