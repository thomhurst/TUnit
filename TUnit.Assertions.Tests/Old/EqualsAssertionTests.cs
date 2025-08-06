namespace TUnit.Assertions.Tests.Old;

public class EqualsAssertionTests
{
    [Test]
    public async Task Assertion_Message_Has_Correct_Expression()
    {
        var one = "1";

        await TUnitAssert.That(async () =>
                await TUnitAssert.That(one).IsEqualTo("2", StringComparison.Ordinal).And.IsNotEqualTo("1").And.IsTypeOf(typeof(string))
            ).ThrowsException()
            .And
            .HasMessageContaining("Assert.That(one).IsEqualTo(\"2\", StringComparison.Ordinal).And.IsNotEqualTo(\"1\", StringComparison.Ord...");
    }

    [Test]
    public async Task Long()
    {
        long zero = 0;
        await TUnitAssert.That(zero).IsEqualTo(0);
    }

    [Test]
    public async Task Short()
    {
        short zero = 0;
        await TUnitAssert.That<long>(zero).IsEqualTo(0);
    }

    [Test]
    public async Task Int_Bad()
    {
        var zero = 1;
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That<long>(zero).IsEqualTo(0));
    }

    [Test]
    public async Task Long_Bad()
    {
        long zero = 1;
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(zero).IsEqualTo(0));
    }

    [Test]
    public async Task Short_Bad()
    {
        short zero = 1;
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That<long>(zero).IsEqualTo(0));
    }
}
