using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class DecimalAssertionTests
{
    // IsZero tests
    [Test]
    public async Task IsZero_WithZero_Passes()
    {
        var value = 0m;
        await Assert.That(value).IsZero();
    }

    [Test]
    public async Task IsZero_WithNonZero_Fails()
    {
        var value = 1.5m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsZero());
    }

    [Test]
    public async Task IsNotZero_WithNonZero_Passes()
    {
        var value = 1.5m;
        await Assert.That(value).IsNotZero();
    }

    [Test]
    public async Task IsNotZero_WithZero_Fails()
    {
        var value = 0m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNotZero());
    }

    // IsWhole tests
    [Test]
    public async Task IsWhole_WithWholeNumber_Passes()
    {
        var value = 42m;
        await Assert.That(value).IsWhole();
    }

    [Test]
    public async Task IsWhole_WithZero_Passes()
    {
        var value = 0m;
        await Assert.That(value).IsWhole();
    }

    [Test]
    public async Task IsWhole_WithNegativeWhole_Passes()
    {
        var value = -100m;
        await Assert.That(value).IsWhole();
    }

    [Test]
    public async Task IsWhole_WithDecimalFraction_Fails()
    {
        var value = 3.14m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsWhole());
    }

    [Test]
    public async Task IsNotWhole_WithDecimalFraction_Passes()
    {
        var value = 3.14m;
        await Assert.That(value).IsNotWhole();
    }

    [Test]
    public async Task IsNotWhole_WithWholeNumber_Fails()
    {
        var value = 42m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNotWhole());
    }

    // IsPositive tests
    [Test]
    public async Task IsPositive_WithPositiveNumber_Passes()
    {
        var value = 42.5m;
        await Assert.That(value).IsPositive();
    }

    [Test]
    public async Task IsPositive_WithZero_Fails()
    {
        var value = 0m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsPositive());
    }

    [Test]
    public async Task IsPositive_WithNegativeNumber_Fails()
    {
        var value = -5m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsPositive());
    }

    [Test]
    public async Task IsNotPositive_WithZero_Passes()
    {
        var value = 0m;
        await Assert.That(value).IsNotPositive();
    }

    [Test]
    public async Task IsNotPositive_WithNegativeNumber_Passes()
    {
        var value = -5m;
        await Assert.That(value).IsNotPositive();
    }

    [Test]
    public async Task IsNotPositive_WithPositiveNumber_Fails()
    {
        var value = 42.5m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNotPositive());
    }

    // IsNegative tests
    [Test]
    public async Task IsNegative_WithNegativeNumber_Passes()
    {
        var value = -42.5m;
        await Assert.That(value).IsNegative();
    }

    [Test]
    public async Task IsNegative_WithZero_Fails()
    {
        var value = 0m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNegative());
    }

    [Test]
    public async Task IsNegative_WithPositiveNumber_Fails()
    {
        var value = 5m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNegative());
    }

    [Test]
    public async Task IsNotNegative_WithZero_Passes()
    {
        var value = 0m;
        await Assert.That(value).IsNotNegative();
    }

    [Test]
    public async Task IsNotNegative_WithPositiveNumber_Passes()
    {
        var value = 5m;
        await Assert.That(value).IsNotNegative();
    }

    [Test]
    public async Task IsNotNegative_WithNegativeNumber_Fails()
    {
        var value = -42.5m;
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).IsNotNegative());
    }

    // HasScale tests
    [Test]
    public async Task HasScale_WithMatchingScale_Passes()
    {
        var value = 123.45m; // Scale is 2
        await Assert.That(value).HasScale(2);
    }

    [Test]
    public async Task HasScale_WithZeroScale_Passes()
    {
        var value = 100m; // Scale is 0
        await Assert.That(value).HasScale(0);
    }

    [Test]
    public async Task HasScale_WithMismatchedScale_Fails()
    {
        var value = 123.456m; // Scale is 3
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).HasScale(2));
    }

    // HasPrecision tests
    [Test]
    public async Task HasPrecision_WithMatchingPrecision_Passes()
    {
        var value = 123.45m; // Precision is 5 (12345)
        await Assert.That(value).HasPrecision(5);
    }

    [Test]
    public async Task HasPrecision_WithSingleDigit_Passes()
    {
        var value = 7m;
        await Assert.That(value).HasPrecision(1);
    }

    [Test]
    public async Task HasPrecision_WithZero_Passes()
    {
        var value = 0m;
        await Assert.That(value).HasPrecision(1);
    }

    [Test]
    public async Task HasPrecision_WithMismatchedPrecision_Fails()
    {
        var value = 123.45m; // Precision is 5
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(value).HasPrecision(3));
    }

    [Test]
    public async Task HasPrecision_WithNegativeNumber_Passes()
    {
        var value = -123.45m; // Precision is 5 (ignoring sign)
        await Assert.That(value).HasPrecision(5);
    }
}
