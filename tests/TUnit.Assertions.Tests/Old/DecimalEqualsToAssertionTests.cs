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
        var decimal1 = 1.0001m;
        var decimal2 = 1.0002m;

        await TUnitAssert.That(decimal1).IsEqualTo(decimal2).Within(0.0001m);
    }

    [Test]
    public async Task Decimal_EqualsTo__With_Tolerance_Failure()
    {
        var decimal1 = 1.0001m;
        var decimal2 = 1.0003m;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(decimal1).IsEqualTo(decimal2).Within(0.0001m));
    }
#endif
}
