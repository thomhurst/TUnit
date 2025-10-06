using System.Globalization;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class CultureInfoAssertionTests
{
    [Test]
    public async Task Test_CultureInfo_IsInvariant()
    {
        var culture = CultureInfo.InvariantCulture;
        await Assert.That(culture).IsInvariant();
    }

    [Test]
    public async Task Test_CultureInfo_IsNotInvariant()
    {
        var culture = new CultureInfo("en-US");
        await Assert.That(culture).IsNotInvariant();
    }

    [Test]
    public async Task Test_CultureInfo_IsNeutralCulture()
    {
        var culture = new CultureInfo("en");
        await Assert.That(culture).IsNeutralCulture();
    }

    [Test]
    public async Task Test_CultureInfo_IsNotNeutralCulture()
    {
        var culture = new CultureInfo("en-US");
        await Assert.That(culture).IsNotNeutralCulture();
    }

    [Test]
    public async Task Test_CultureInfo_IsEnglish()
    {
        var culture = new CultureInfo("en-US");
        await Assert.That(culture).IsEnglish();
    }

    [Test]
    public async Task Test_CultureInfo_IsNotEnglish()
    {
        var culture = new CultureInfo("fr-FR");
        await Assert.That(culture).IsNotEnglish();
    }

    [Test]
    public async Task Test_CultureInfo_IsRightToLeft()
    {
        var culture = new CultureInfo("ar-SA");
        await Assert.That(culture).IsRightToLeft();
    }

    [Test]
    public async Task Test_CultureInfo_IsLeftToRight()
    {
        var culture = new CultureInfo("en-US");
        await Assert.That(culture).IsLeftToRight();
    }

    [Test]
    public async Task Test_CultureInfo_IsReadOnly()
    {
        var culture = CultureInfo.ReadOnly(new CultureInfo("en-US"));
        await Assert.That(culture).IsReadOnly();
    }
}