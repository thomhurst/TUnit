using System.Globalization;
using TUnit.Assertions.Assertions.Strings;

namespace TUnit.Assertions.Tests;

public class ParseAssertionTests
{
    [Test]
    public async Task IsParsableInto_Int_ValidString()
    {
        await Assert.That("123").IsParsableInto<int>();
    }

    [Test]
    public async Task IsNotParsableInto_Int_InvalidString()
    {
        await Assert.That("abc").IsNotParsableInto<int>();
    }

    [Test]
    public async Task WhenParsedInto_Int_CanChain()
    {
        await Assert.That("100")
            .WhenParsedInto<int>()
            .IsGreaterThan(50);
    }

    [Test]
    public async Task IsParsableInto_WithFormatProvider()
    {
        var germanCulture = new CultureInfo("de-DE");
        await Assert.That("123,45")
            .IsParsableInto<double>()
            .WithFormatProvider(germanCulture);
    }

    [Test]
    public async Task IsParsableInto_Guid()
    {
        await Assert.That("12345678-1234-1234-1234-123456789012")
            .IsParsableInto<Guid>();
    }

    [Test]
    public async Task IsParsableInto_DateTime()
    {
        await Assert.That("2024-01-01").IsParsableInto<DateTime>();
    }

    [Test]
    public async Task WhenParsedInto_Bool_Chaining()
    {
        await Assert.That("true")
            .WhenParsedInto<bool>()
            .IsTrue();
    }

    // Tests for issue #3425: Assertions before conversion are not checked
    [Test]
    public async Task WhenParsedInto_WithAnd_PreviousAssertion_ShouldFail()
    {
        // HasLength(4) should fail because "123" has length 3
        var exception = await Assert.That(async () =>
        {
            var sut = "123";
            await Assert.That(sut)
                .HasLength(4)
                .And
                .WhenParsedInto<int>()
                .IsEqualTo(123);
        }).ThrowsException().And.HasMessageContaining("HasLength(4)");
    }

    [Test]
    public async Task WhenParsedInto_WithAnd_PreviousAssertion_ShouldPass()
    {
        // Both assertions should pass
        var sut = "123";
        await Assert.That(sut)
            .HasLength(3)
            .And
            .WhenParsedInto<int>()
            .IsEqualTo(123);
    }

    [Test]
    public async Task WhenParsedInto_WithAnd_MultiplePreviousAssertions_ShouldFail()
    {
        // IsNotEmpty should pass, but HasLength(4) should fail
        var exception = await Assert.That(async () =>
        {
            var sut = "123";
            await Assert.That(sut)
                .IsNotEmpty()
                .And
                .HasLength(4)
                .And
                .WhenParsedInto<int>()
                .IsGreaterThan(100);
        }).ThrowsException().And.HasMessageContaining("HasLength(4)");
    }

    [Test]
    public async Task WhenParsedInto_WithAnd_ChainingAfterParse()
    {
        // Previous assertion should be checked before parsing
        var sut = "100";
        await Assert.That(sut)
            .HasLength(3)
            .And
            .WhenParsedInto<int>()
            .IsGreaterThan(50)
            .And
            .IsLessThan(200);
    }

    [Test]
    public async Task WhenParsedInto_WithAnd_FirstAssertionInChainFails()
    {
        // First assertion should fail, never reach parsing
        var exception = await Assert.That(async () =>
        {
            var sut = "123";
            await Assert.That(sut)
                .StartsWith("4")
                .And
                .WhenParsedInto<int>()
                .IsEqualTo(123);
        }).ThrowsException().And.HasMessageContaining("StartsWith");
    }

    [Test]
    public async Task WhenParsedInto_WithAnd_ParsedAssertionFails()
    {
        // Previous assertion passes, but parsed assertion fails
        var exception = await Assert.That(async () =>
        {
            var sut = "123";
            await Assert.That(sut)
                .HasLength(3)
                .And
                .WhenParsedInto<int>()
                .IsEqualTo(456);
        }).ThrowsException().And.HasMessageContaining("IsEqualTo");
    }

    [Test]
    public async Task WhenParsedInto_AssertMultiple_AllAssertionsChecked()
    {
        // In Assert.Multiple, both failing assertions should be captured
        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                var sut = "123";
                await Assert.That(sut)
                    .HasLength(4)
                    .And
                    .WhenParsedInto<int>()
                    .IsEqualTo(456);
            }
        }).ThrowsException();

        // Should have recorded the HasLength failure
        await Assert.That(exception.Message).Contains("HasLength");
    }

    [Test]
    public async Task WhenParsedInto_ComplexChain_AllChecked()
    {
        // Complex chain: string assertions, parse, int assertions
        var sut = "1234";
        await Assert.That(sut)
            .HasLength(4)
            .And
            .StartsWith("1")
            .And
            .EndsWith("4")
            .And
            .WhenParsedInto<int>()
            .IsGreaterThan(1000)
            .And
            .IsLessThan(2000)
            .And
            .IsBetween(1200, 1300);
    }

    [Test]
    public async Task WhenParsedInto_ComplexChain_MiddleStringAssertionFails()
    {
        // Should fail at EndsWith before reaching parsing
        var exception = await Assert.That(async () =>
        {
            var sut = "1234";
            await Assert.That(sut)
                .HasLength(4)
                .And
                .StartsWith("1")
                .And
                .EndsWith("5")
                .And
                .WhenParsedInto<int>()
                .IsGreaterThan(1000);
        }).ThrowsException().And.HasMessageContaining("EndsWith");
    }
}
