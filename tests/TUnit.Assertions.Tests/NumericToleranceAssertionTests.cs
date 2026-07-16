using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class NumericToleranceAssertionTests
{
    // ==========================================
    // IsCloseTo tests - double
    // ==========================================

    [Test]
    public async Task Double_IsCloseTo_Within_Tolerance_Passes()
    {
        double value = 10.5;
        await Assert.That(value).IsCloseTo(10.0, 0.5);
    }

    [Test]
    public async Task Double_IsCloseTo_Exact_Match_Passes()
    {
        double value = 3.14;
        await Assert.That(value).IsCloseTo(3.14, 0.0);
    }

    [Test]
    public async Task Double_IsCloseTo_Outside_Tolerance_Fails()
    {
        double value = 10.0;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsCloseTo(12.0, 1.0));
    }

    [Test]
    public async Task Double_IsCloseTo_NaN_Both_Passes()
    {
        await Assert.That(double.NaN).IsCloseTo(double.NaN, 0.1);
    }

    [Test]
    public async Task Double_IsCloseTo_NaN_Actual_Fails()
    {
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(double.NaN).IsCloseTo(1.0, 0.1));
    }

    [Test]
    public async Task Double_IsCloseTo_Negative_Values_Passes()
    {
        double value = -5.1;
        await Assert.That(value).IsCloseTo(-5.0, 0.2);
    }

    // ==========================================
    // IsCloseTo tests - float
    // ==========================================

    [Test]
    public async Task Float_IsCloseTo_Within_Tolerance_Passes()
    {
        float value = 10.5f;
        await Assert.That(value).IsCloseTo(10.0f, 0.5f);
    }

    [Test]
    public async Task Float_IsCloseTo_Outside_Tolerance_Fails()
    {
        float value = 10.0f;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsCloseTo(12.0f, 1.0f));
    }

    [Test]
    public async Task Float_IsCloseTo_NaN_Both_Passes()
    {
        await Assert.That(float.NaN).IsCloseTo(float.NaN, 0.1f);
    }

    // ==========================================
    // IsCloseTo tests - int
    // ==========================================

    [Test]
    public async Task Int_IsCloseTo_Within_Tolerance_Passes()
    {
        int value = 105;
        await Assert.That(value).IsCloseTo(100, 5);
    }

    [Test]
    public async Task Int_IsCloseTo_Exact_Match_Passes()
    {
        int value = 42;
        await Assert.That(value).IsCloseTo(42, 0);
    }

    [Test]
    public async Task Int_IsCloseTo_Outside_Tolerance_Fails()
    {
        int value = 100;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsCloseTo(110, 5));
    }

    // ==========================================
    // IsCloseTo tests - long
    // ==========================================

    [Test]
    public async Task Long_IsCloseTo_Within_Tolerance_Passes()
    {
        long value = 1000000005L;
        await Assert.That(value).IsCloseTo(1000000000L, 10L);
    }

    [Test]
    public async Task Long_IsCloseTo_Outside_Tolerance_Fails()
    {
        long value = 100L;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsCloseTo(200L, 50L));
    }

    // ==========================================
    // IsCloseTo tests - decimal
    // ==========================================

    [Test]
    public async Task Decimal_IsCloseTo_Within_Tolerance_Passes()
    {
        decimal value = 10.05m;
        await Assert.That(value).IsCloseTo(10.0m, 0.1m);
    }

    [Test]
    public async Task Decimal_IsCloseTo_Outside_Tolerance_Fails()
    {
        decimal value = 10.0m;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsCloseTo(12.0m, 1.0m));
    }

    // ==========================================
    // IsWithinPercentOf tests - double
    // ==========================================

    [Test]
    public async Task Double_IsWithinPercentOf_Passes()
    {
        double value = 105.0;
        await Assert.That(value).IsWithinPercentOf(100.0, 10.0);
    }

    [Test]
    public async Task Double_IsWithinPercentOf_Exact_Match_Passes()
    {
        double value = 100.0;
        await Assert.That(value).IsWithinPercentOf(100.0, 0.0);
    }

    [Test]
    public async Task Double_IsWithinPercentOf_At_Boundary_Passes()
    {
        double value = 110.0;
        await Assert.That(value).IsWithinPercentOf(100.0, 10.0);
    }

    [Test]
    public async Task Double_IsWithinPercentOf_Outside_Fails()
    {
        double value = 120.0;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(100.0, 10.0));
    }

    [Test]
    public async Task Double_IsWithinPercentOf_Negative_Expected_Passes()
    {
        double value = -95.0;
        await Assert.That(value).IsWithinPercentOf(-100.0, 10.0);
    }

    [Test]
    public async Task Double_IsWithinPercentOf_NaN_Both_Passes()
    {
        await Assert.That(double.NaN).IsWithinPercentOf(double.NaN, 10.0);
    }

    // ==========================================
    // IsWithinPercentOf tests - float
    // ==========================================

    [Test]
    public async Task Float_IsWithinPercentOf_Passes()
    {
        float value = 105.0f;
        await Assert.That(value).IsWithinPercentOf(100.0f, 10.0f);
    }

    [Test]
    public async Task Float_IsWithinPercentOf_Outside_Fails()
    {
        float value = 120.0f;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(100.0f, 10.0f));
    }

    // ==========================================
    // IsWithinPercentOf tests - int
    // ==========================================

    [Test]
    public async Task Int_IsWithinPercentOf_Passes()
    {
        int value = 105;
        await Assert.That(value).IsWithinPercentOf(100, 10.0);
    }

    [Test]
    public async Task Int_IsWithinPercentOf_Outside_Fails()
    {
        int value = 120;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(100, 10.0));
    }

    // ==========================================
    // IsWithinPercentOf tests - long
    // ==========================================

    [Test]
    public async Task Long_IsWithinPercentOf_Passes()
    {
        long value = 1050L;
        await Assert.That(value).IsWithinPercentOf(1000L, 10.0);
    }

    [Test]
    public async Task Long_IsWithinPercentOf_Outside_Fails()
    {
        long value = 1200L;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(1000L, 10.0));
    }

    // ==========================================
    // IsWithinPercentOf tests - decimal
    // ==========================================

    [Test]
    public async Task Decimal_IsWithinPercentOf_Passes()
    {
        decimal value = 105.0m;
        await Assert.That(value).IsWithinPercentOf(100.0m, 10.0m);
    }

    [Test]
    public async Task Decimal_IsWithinPercentOf_Outside_Fails()
    {
        decimal value = 120.0m;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(100.0m, 10.0m));
    }

    // ==========================================
    // Edge cases
    // ==========================================

    [Test]
    public async Task Double_IsCloseTo_Zero_Expected_Passes()
    {
        double value = 0.001;
        await Assert.That(value).IsCloseTo(0.0, 0.01);
    }

    [Test]
    public async Task Int_IsWithinPercentOf_Zero_Expected_Fails()
    {
        // 10% of 0 is 0, so only exact match passes
        int value = 1;
        await Assert.ThrowsAsync<AssertionException>(
            async () => await Assert.That(value).IsWithinPercentOf(0, 10.0));
    }

    [Test]
    public async Task Int_IsWithinPercentOf_Zero_Expected_Zero_Actual_Passes()
    {
        // 0 is within any percent of 0
        int value = 0;
        await Assert.That(value).IsWithinPercentOf(0, 10.0);
    }
}
