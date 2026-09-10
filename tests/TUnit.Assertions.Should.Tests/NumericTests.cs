using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class NumericTests
{
    [Test]
    public async Task Int_BeBetween()
    {
        await 5.Should().BeBetween(1, 10);
    }

    [Test]
    public async Task Int_BeCloseTo_within_tolerance()
    {
        await 100.Should().BeCloseTo(102, 5);
    }

    [Test]
    public async Task Int_BeWithinPercentOf()
    {
        await 105.Should().BeWithinPercentOf(100, 10);
    }

    [Test]
    public async Task Long_BeEqualTo()
    {
        await 12345678901234L.Should().BeEqualTo(12345678901234L);
    }

    [Test]
    public async Task Decimal_BeEqualTo()
    {
        await 3.14m.Should().BeEqualTo(3.14m);
    }

    [Test]
    public async Task Decimal_BeCloseTo()
    {
        await 3.14m.Should().BeCloseTo(3.15m, 0.02m);
    }

    [Test]
    public async Task Double_BeEqualTo()
    {
        await 1.5.Should().BeEqualTo(1.5);
    }

    [Test]
    public async Task Double_BeCloseTo()
    {
        await 0.1.Should().BeCloseTo(0.10001, 0.001);
    }

    [Test]
    public async Task Float_BeEqualTo()
    {
        await 2.5f.Should().BeEqualTo(2.5f);
    }

    [Test]
    public async Task Int_BeBetween_failure_throws()
    {
        var ex = await Assert.That(async () => await 50.Should().BeBetween(1, 10))
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        await Assert.That(ex.Message).Contains("BeBetween");
    }

    [Test]
    public async Task Int_NotBeEqualTo()
    {
        await 5.Should().NotBeEqualTo(7);
    }
}
