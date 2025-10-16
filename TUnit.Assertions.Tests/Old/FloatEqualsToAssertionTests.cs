namespace TUnit.Assertions.Tests.Old;

public class FloatEqualsToAssertionTests
{
    [Test]
    public async Task Float_EqualsTo_Success()
    {
        var float1 = 1.1f;
        var float2 = 1.1f;

        await TUnitAssert.That(float1).IsEqualTo(float2);
    }

    [Test]
    public async Task Float_EqualsTo_Failure()
    {
        var float1 = 1.1f;
        var float2 = 1.2f;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(float1).IsEqualTo(float2));
    }

    [Test]
    public async Task Float_NaN_EqualsTo_NaN_Success()
    {
        await TUnitAssert.That(float.NaN).IsEqualTo(float.NaN);
    }

    [Test]
    public async Task Float_PositiveInfinity_EqualsTo_PositiveInfinity_Success()
    {
        await TUnitAssert.That(float.PositiveInfinity).IsEqualTo(float.PositiveInfinity);
    }

    [Test]
    public async Task Float_NegativeInfinity_EqualsTo_NegativeInfinity_Success()
    {
        await TUnitAssert.That(float.NegativeInfinity).IsEqualTo(float.NegativeInfinity);
    }

#if NET
    [Test]
    public async Task Float_EqualsTo__With_Tolerance_Success()
    {
        var float1 = 1.1f;
        var float2 = 1.2f;

        await TUnitAssert.That(float1).IsEqualTo(float2).Within(0.1f);
    }

    [Test]
    public async Task Float_EqualsTo__With_Tolerance_Failure()
    {
        var float1 = 1.1f;
        var float2 = 1.3f;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(float1).IsEqualTo(float2).Within(0.1f));
    }

    [Test]
    public async Task Float_NaN_EqualsTo_NaN_With_Tolerance_Success()
    {
        const float tolerance = 0.001f;

        await TUnitAssert.That(float.NaN).IsEqualTo(float.NaN).Within(tolerance);
    }

    [Test]
    public async Task Float_NaN_EqualsTo_Number_With_Tolerance_Failure()
    {
        const float tolerance = 0.001f;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(float.NaN).IsEqualTo(1.0f).Within(tolerance));
    }

    [Test]
    public async Task Float_Number_EqualsTo_NaN_With_Tolerance_Failure()
    {
        const float tolerance = 0.001f;

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
            await TUnitAssert.That(1.0f).IsEqualTo(float.NaN).Within(tolerance));
    }
#endif
}
