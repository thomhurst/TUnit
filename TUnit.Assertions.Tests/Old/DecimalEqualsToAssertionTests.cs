namespace TUnit.Assertions.Tests.Old;

public class DecimalEqualsToAssertionTests
{
    [Test]
    public async Task Decimal_EqualsTo_Success()
    {
        var double1 = 1.0001m;
        var double2 = 1.0001m;

        await TUnitAssert.That(double1).IsEqualTo(double2);
    }

    [Test]
    public async Task Decimal_EqualsTo_Failure()
    {
        var double1 = 1.0001m;
        var double2 = 1.0002m;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(double1).IsEqualTo(double2));
    }

#if NET
    [Test]
    public async Task Decimal_EqualsTo__With_Tolerance_Success()
    {
        var double1 = 1.0001d;
        var double2 = 1.0002d;
        
        await TUnitAssert.That(double1).IsEqualTo(double2).Within(0.0001);
    }
    
    [Test]
    public async Task Decimal_EqualsTo__With_Tolerance_Failure()
    {
        var double1 = 1.0001d;
        var double2 = 1.0003d;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(double1).IsEqualTo(double2).Within(0.0001));
    }
#endif
}
