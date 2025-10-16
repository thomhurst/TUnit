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

    [Test]
    public async Task Double_NaN_EqualsTo_NaN_Success()
    {
        await TUnitAssert.That(double.NaN).IsEqualTo(double.NaN);
    }

    [Test]
    public async Task Double_PositiveInfinity_EqualsTo_PositiveInfinity_Success()
    {
        await TUnitAssert.That(double.PositiveInfinity).IsEqualTo(double.PositiveInfinity);
    }

    [Test]
    public async Task Double_NegativeInfinity_EqualsTo_NegativeInfinity_Success()
    {
        await TUnitAssert.That(double.NegativeInfinity).IsEqualTo(double.NegativeInfinity);
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

    [Test]
    public async Task Double_NaN_EqualsTo_NaN_With_Tolerance_Success()
    {
        const double tolerance = 0.001;

        await TUnitAssert.That(double.NaN).IsEqualTo(double.NaN).Within(tolerance);
    }

    [Test]
    public async Task Double_NaN_EqualsTo_Number_With_Tolerance_Failure()
    {
        const double tolerance = 0.001;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(double.NaN).IsEqualTo(1.0).Within(tolerance));
    }

    [Test]
    public async Task Double_Number_EqualsTo_NaN_With_Tolerance_Failure()
    {
        const double tolerance = 0.001;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(1.0).IsEqualTo(double.NaN).Within(tolerance));
    }
#endif
}
