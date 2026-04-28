using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

/// <summary>
/// Verifies the generator picks up assertions produced by <c>[GenerateAssertion]</c>
/// (e.g., <c>IsZero</c>, <c>IsEven</c>, <c>IsOdd</c> on <c>int</c>) — these don't carry an
/// <c>[AssertionExtension]</c> attribute on the underlying assertion class, so they need
/// the extension-method-scanning code path to be discovered.
/// </summary>
public class GenerateAssertionTests
{
    [Test]
    public async Task Int_BeZero()
    {
        await 0.Should().BeZero();
    }

    [Test]
    public async Task Int_NotBeZero()
    {
        await 5.Should().NotBeZero();
    }

    [Test]
    public async Task Int_BeEven()
    {
        await 4.Should().BeEven();
    }

    [Test]
    public async Task Int_BeOdd()
    {
        await 3.Should().BeOdd();
    }

    [Test]
    public async Task Double_BeZero()
    {
        await 0.0.Should().BeZero();
    }

    [Test]
    public async Task Bool_BeTrue_via_GenerateAssertion()
    {
        // BoolAssertions exposes IsTrue/IsFalse via [GenerateAssertion(InlineMethodBody = true)].
        await true.Should().BeTrue();
    }

    [Test]
    public async Task Bool_BeFalse_via_GenerateAssertion()
    {
        await false.Should().BeFalse();
    }

    [Test]
    public async Task Range_BeAll()
    {
        await Range.All.Should().BeAll();
    }

    [Test]
    public async Task BeZero_failure_reports_method_name()
    {
        var ex = await Assert.That(async () => await 5.Should().BeZero())
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        await Assert.That(ex.Message).Contains(".BeZero()");
    }
}
