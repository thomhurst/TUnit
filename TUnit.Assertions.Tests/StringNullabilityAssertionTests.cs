using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for string assertions that accept null values (IsNullOrEmpty, IsNullOrWhiteSpace)
/// </summary>
public class StringNullabilityAssertionTests
{
    [Test]
    public async Task IsNullOrEmpty_WithNullString_Passes()
    {
        string? nullString = null;
        await Assert.That(nullString).IsNullOrEmpty();
    }

    [Test]
    public async Task IsNullOrEmpty_WithEmptyString_Passes()
    {
        var emptyString = "";
        await Assert.That(emptyString).IsNullOrEmpty();
    }

    [Test]
    public async Task IsNullOrEmpty_WithNonEmptyString_Fails()
    {
        var value = "Hello";
        await Assert.That(async () => await Assert.That(value).IsNullOrEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNullOrEmpty_WithWhitespace_Fails()
    {
        var value = " ";
        await Assert.That(async () => await Assert.That(value).IsNullOrEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNullOrWhiteSpace_WithNullString_Passes()
    {
        string? nullString = null;
        await Assert.That(nullString).IsNullOrWhiteSpace();
    }

    [Test]
    public async Task IsNullOrWhiteSpace_WithEmptyString_Passes()
    {
        var emptyString = "";
        await Assert.That(emptyString).IsNullOrWhiteSpace();
    }

    [Test]
    public async Task IsNullOrWhiteSpace_WithWhitespace_Passes()
    {
        var whitespace = "   ";
        await Assert.That(whitespace).IsNullOrWhiteSpace();
    }

    [Test]
    public async Task IsNullOrWhiteSpace_WithNonEmptyString_Fails()
    {
        var value = "Hello";
        await Assert.That(async () => await Assert.That(value).IsNullOrWhiteSpace())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotNullOrEmpty_WithNullString_Fails()
    {
        string? nullString = null;
        await Assert.That(async () => await Assert.That(nullString).IsNotNullOrEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotNullOrEmpty_WithEmptyString_Fails()
    {
        var emptyString = "";
        await Assert.That(async () => await Assert.That(emptyString).IsNotNullOrEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotNullOrEmpty_WithNonEmptyString_Passes()
    {
        var value = "Hello";
        await Assert.That(value).IsNotNullOrEmpty();
    }
}
