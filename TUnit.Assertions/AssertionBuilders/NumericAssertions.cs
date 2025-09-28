using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace TUnit.Assertions.AssertionBuilders;

// Extension methods for numeric assertions
public static class NumericAssertionExtensions
{
    // Integer assertions
    public static CustomAssertion<int> IsPositive(this AssertionBuilder<int> builder)
    {
        return new CustomAssertion<int>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    // Alias for backward compatibility
    public static CustomAssertion<int> Positive(this AssertionBuilder<int> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<int> IsNegative(this AssertionBuilder<int> builder)
    {
        return new CustomAssertion<int>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    // Alias for backward compatibility
    public static CustomAssertion<int> Negative(this AssertionBuilder<int> builder)
    {
        return IsNegative(builder);
    }

    public static CustomAssertion<int> IsZero(this AssertionBuilder<int> builder)
    {
        return new CustomAssertion<int>(builder.ActualValueProvider,
            value => value == 0,
            "Expected value to be zero");
    }

    // Alias for backward compatibility
    public static CustomAssertion<int> Zero(this AssertionBuilder<int> builder)
    {
        return IsZero(builder);
    }

    // Long assertions
    public static CustomAssertion<long> IsPositive(this AssertionBuilder<long> builder)
    {
        return new CustomAssertion<long>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<long> Positive(this AssertionBuilder<long> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<long> IsNegative(this AssertionBuilder<long> builder)
    {
        return new CustomAssertion<long>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    public static CustomAssertion<long> Negative(this AssertionBuilder<long> builder)
    {
        return IsNegative(builder);
    }

    // Double assertions
    public static CustomAssertion<double> IsPositive(this AssertionBuilder<double> builder)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<double> Positive(this AssertionBuilder<double> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<double> IsNegative(this AssertionBuilder<double> builder)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    public static CustomAssertion<double> Negative(this AssertionBuilder<double> builder)
    {
        return IsNegative(builder);
    }

    public static CustomAssertion<double> IsNaN(this AssertionBuilder<double> builder)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => double.IsNaN(value),
            "Expected value to be NaN");
    }

    public static CustomAssertion<double> IsInfinity(this AssertionBuilder<double> builder)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => double.IsInfinity(value),
            "Expected value to be infinity");
    }

    // Float assertions
    public static CustomAssertion<float> IsPositive(this AssertionBuilder<float> builder)
    {
        return new CustomAssertion<float>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<float> Positive(this AssertionBuilder<float> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<float> IsNegative(this AssertionBuilder<float> builder)
    {
        return new CustomAssertion<float>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    public static CustomAssertion<float> Negative(this AssertionBuilder<float> builder)
    {
        return IsNegative(builder);
    }

    // Decimal assertions
    public static CustomAssertion<decimal> IsPositive(this AssertionBuilder<decimal> builder)
    {
        return new CustomAssertion<decimal>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<decimal> Positive(this AssertionBuilder<decimal> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<decimal> IsNegative(this AssertionBuilder<decimal> builder)
    {
        return new CustomAssertion<decimal>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    public static CustomAssertion<decimal> Negative(this AssertionBuilder<decimal> builder)
    {
        return IsNegative(builder);
    }

    // Short assertions
    public static CustomAssertion<short> IsPositive(this AssertionBuilder<short> builder)
    {
        return new CustomAssertion<short>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<short> Positive(this AssertionBuilder<short> builder)
    {
        return IsPositive(builder);
    }

    public static CustomAssertion<short> IsNegative(this AssertionBuilder<short> builder)
    {
        return new CustomAssertion<short>(builder.ActualValueProvider,
            value => value < 0,
            "Expected value to be negative");
    }

    public static CustomAssertion<short> Negative(this AssertionBuilder<short> builder)
    {
        return IsNegative(builder);
    }

    // Byte assertions (byte is always non-negative)
    public static CustomAssertion<byte> IsPositive(this AssertionBuilder<byte> builder)
    {
        return new CustomAssertion<byte>(builder.ActualValueProvider,
            value => value > 0,
            "Expected value to be positive");
    }

    public static CustomAssertion<byte> Positive(this AssertionBuilder<byte> builder)
    {
        return IsPositive(builder);
    }

#if NET7_0_OR_GREATER
    // Generic numeric assertions for INumber<T> types (.NET 7+)
    public static CustomAssertion<T> IsPositive<T>(this AssertionBuilder<T> builder)
        where T : INumber<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            value => value > T.Zero,
            "Expected value to be positive");
    }

    public static CustomAssertion<T> IsNegative<T>(this AssertionBuilder<T> builder)
        where T : INumber<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            value => value < T.Zero,
            "Expected value to be negative");
    }

    public static CustomAssertion<T> IsZero<T>(this AssertionBuilder<T> builder)
        where T : INumber<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            value => value == T.Zero,
            "Expected value to be zero");
    }
#endif
}