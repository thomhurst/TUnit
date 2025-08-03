namespace TUnit.Assertions.Tests.Old;

public class DoubleEqualsToAssertionTests
{
    [Test]
    public async Task Double_EqualsTo_Success()
    {
        var double1 = 1.1d;
        var double2 = 1.1d;

        await TUnitAssert.That(double1).IsEqualTo(double2);
    }

    [Test]
    public async Task Double_EqualsTo_Failure()
    {
        var double1 = 1.1d;
        var double2 = 1.2d;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(double1).IsEqualTo(double2));
    }

#if NET
    [Test]
    public async Task Double_EqualsTo__With_Tolerance_Success()
    {
        var double1 = 1.1d;
        var double2 = 1.2d;
        
        await TUnitAssert.That(double1).IsEqualTo(double2).Within(0.1);
    }

    [Test]
    public async Task Double_EqualsTo__With_Tolerance_Failure()
    {
        var double1 = 1.1d;
        var double2 = 1.3d;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(double1).IsEqualTo(double2).Within(0.1));
    }
#endif
}
