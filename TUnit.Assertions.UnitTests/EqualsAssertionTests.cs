using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class EqualsAssertionTests
{
    [Test]
    public void Assertion_Message_Has_Correct_Expression()
    {
        var one = "1";
        NUnitAssert.That(async () =>
                await TUnitAssert.That(one).Is.EqualTo("2", StringComparison.Ordinal).And.Is.Not.EqualTo("1").And.Is
                    .TypeOf<string>(),
            Throws.Exception.Message.Contain("Assert.That(one).Is.EqualTo(\"2\", StringComparison.Ordinal).And.Is.Not.EqualTo(\"1\").And.Is.TypeOf(System.String")
        );
    }
    
    [Test]
    public async Task Long()
    {
        long zero = 0;
        await TUnitAssert.That(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Short()
    {
        short zero = 0;
        await TUnitAssert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Int_Bad()
    {
        int zero = 1;
        await TUnitAssert.That<long>(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Long_Bad()
    {
        long zero = 1;
        await TUnitAssert.That(zero).Is.EqualTo(0);
    }
    
    [Test]
    public async Task Short_Bad()
    {
        short zero = 1;
        await TUnitAssert.That<long>(zero).Is.EqualTo(0);
    }
}