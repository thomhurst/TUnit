using System.Globalization;

namespace TUnit.TestProject;

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
}