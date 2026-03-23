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

    [Test]
    public async Task HasMinLength_Passes_When_At_Minimum()
    {
        await Assert.That("Hello").HasMinLength(5);
    }

    [Test]
    public async Task HasMinLength_Passes_When_Above_Minimum()
    {
        await Assert.That("Hello, World!").HasMinLength(5);
    }

    [Test]
    public async Task HasMinLength_Fails_When_Below_Minimum()
    {
        await Assert.That(async () => await Assert.That("Hi").HasMinLength(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("minimum length")
            .And.HasMessageContaining("found length 2");
    }

    [Test]
    public async Task HasMinLength_Fails_For_Null()
    {
        string? str = null;
        await Assert.That(async () => await Assert.That(str!).HasMinLength(1))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("null");
    }

    [Test]
    public async Task HasMaxLength_Passes_When_At_Maximum()
    {
        await Assert.That("Hello").HasMaxLength(5);
    }

    [Test]
    public async Task HasMaxLength_Passes_When_Below_Maximum()
    {
        await Assert.That("Hi").HasMaxLength(5);
    }

    [Test]
    public async Task HasMaxLength_Fails_When_Above_Maximum()
    {
        await Assert.That(async () => await Assert.That("Hello, World!").HasMaxLength(5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("maximum length")
            .And.HasMessageContaining("found length 13");
    }

    [Test]
    public async Task HasMaxLength_Fails_For_Null()
    {
        string? str = null;
        await Assert.That(async () => await Assert.That(str!).HasMaxLength(10))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("null");
    }

    [Test]
    public async Task HasLengthBetween_Passes_When_Within_Range()
    {
        await Assert.That("Hello").HasLengthBetween(3, 10);
    }

    [Test]
    public async Task HasLengthBetween_Passes_When_At_Lower_Bound()
    {
        await Assert.That("Hello").HasLengthBetween(5, 10);
    }

    [Test]
    public async Task HasLengthBetween_Passes_When_At_Upper_Bound()
    {
        await Assert.That("Hello").HasLengthBetween(1, 5);
    }

    [Test]
    public async Task HasLengthBetween_Fails_When_Below_Range()
    {
        await Assert.That(async () => await Assert.That("Hi").HasLengthBetween(5, 10))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("between")
            .And.HasMessageContaining("found length 2");
    }

    [Test]
    public async Task HasLengthBetween_Fails_When_Above_Range()
    {
        await Assert.That(async () => await Assert.That("Hello, World!").HasLengthBetween(1, 5))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("between")
            .And.HasMessageContaining("found length 13");
    }

    [Test]
    public async Task HasLengthBetween_Fails_For_Null()
    {
        string? str = null;
        await Assert.That(async () => await Assert.That(str!).HasLengthBetween(1, 10))
            .Throws<TUnitAssertionException>()
            .And.HasMessageContaining("null");
    }

    [Test]
    public async Task HasLengthBetween_Throws_When_Min_Greater_Than_Max()
    {
        await Assert.That(async () => await Assert.That("Hello").HasLengthBetween(10, 3))
            .Throws<ArgumentOutOfRangeException>()
            .And.HasMessageContaining("minLength");
    }

    [Test]
    public async Task HasMinLength_And_HasMaxLength_Chained()
    {
        await Assert.That("Hello").HasMinLength(3).And.HasMaxLength(10);
    }
}
