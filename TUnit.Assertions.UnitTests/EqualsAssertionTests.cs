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
    public void Long()
    {
        long zero = 0;
        TUnitAssert.That(zero);
    }
    
    [Test]
    public void Short()
    {
        short zero = 0;
        TUnitAssert.That<long>(zero);
    }
    
    [Test]
    public void Int_Bad()
    {
        int zero = 1;
        TUnitAssert.That<long>(zero);
    }
    
    [Test]
    public void Long_Bad()
    {
        long zero = 1;
        TUnitAssert.That(zero);
    }
    
    [Test]
    public void Short_Bad()
    {
        short zero = 1;
        TUnitAssert.That<long>(zero);
    }
}