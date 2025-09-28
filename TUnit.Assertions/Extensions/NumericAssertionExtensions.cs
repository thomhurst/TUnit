using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for numeric type assertions
/// </summary>
public static class NumericAssertionExtensions
{
#if NET7_0_OR_GREATER
    // === GENERIC NUMERIC ASSERTIONS for INumber<T> ===
    public static ComparisonAssertion<T> IsPositive<T>(this ValueAssertionBuilder<T> builder)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider,
            T.Zero,
            ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<T> IsNegative<T>(this ValueAssertionBuilder<T> builder)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider,
            T.Zero,
            ComparisonType.LessThan);
    }

    public static CustomAssertion<T> IsZero<T>(this ValueAssertionBuilder<T> builder)
        where T : System.Numerics.INumber<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            value => value != null && value == T.Zero,
            "Expected value to be zero");
    }

    public static CustomAssertion<T> IsNotZero<T>(this ValueAssertionBuilder<T> builder)
        where T : System.Numerics.INumber<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            value => value != null && value != T.Zero,
            "Expected value to not be zero");
    }

    public static ComparisonAssertion<T> IsGreaterThan<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<T> IsGreaterThanOrEqualTo<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<T> IsLessThan<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<T> IsLessThanOrEqualTo<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : System.Numerics.INumber<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }
#endif
    // === INT EXTENSIONS ===
    public static ComparisonAssertion<int> IsGreaterThan(this ValueAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<int> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<int> IsLessThan(this ValueAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<int> IsLessThanOrEqualTo(this ValueAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<int> IsPositive(this ValueAssertionBuilder<int> builder)
    {
        return builder.IsGreaterThan(0);
    }

    public static ComparisonAssertion<int> IsNegative(this ValueAssertionBuilder<int> builder)
    {
        return builder.IsLessThan(0);
    }

    // Also for DualAssertionBuilder (value-returning delegates)
    public static ComparisonAssertion<int> IsGreaterThan(this DualAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<int> IsGreaterThanOrEqualTo(this DualAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<int> IsLessThan(this DualAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<int> IsLessThanOrEqualTo(this DualAssertionBuilder<int> builder, int value)
    {
        return new ComparisonAssertion<int>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<int> IsPositive(this DualAssertionBuilder<int> builder)
    {
        return builder.IsGreaterThan(0);
    }

    public static ComparisonAssertion<int> IsNegative(this DualAssertionBuilder<int> builder)
    {
        return builder.IsLessThan(0);
    }

    // === LONG EXTENSIONS ===
    public static ComparisonAssertion<long> IsGreaterThan(this ValueAssertionBuilder<long> builder, long value)
    {
        return new ComparisonAssertion<long>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<long> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<long> builder, long value)
    {
        return new ComparisonAssertion<long>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<long> IsLessThan(this ValueAssertionBuilder<long> builder, long value)
    {
        return new ComparisonAssertion<long>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<long> IsLessThanOrEqualTo(this ValueAssertionBuilder<long> builder, long value)
    {
        return new ComparisonAssertion<long>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<long> IsPositive(this ValueAssertionBuilder<long> builder)
    {
        return builder.IsGreaterThan(0L);
    }

    public static ComparisonAssertion<long> IsNegative(this ValueAssertionBuilder<long> builder)
    {
        return builder.IsLessThan(0L);
    }

    // === DOUBLE EXTENSIONS ===
    public static ComparisonAssertion<double> IsGreaterThan(this ValueAssertionBuilder<double> builder, double value)
    {
        return new ComparisonAssertion<double>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<double> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<double> builder, double value)
    {
        return new ComparisonAssertion<double>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<double> IsLessThan(this ValueAssertionBuilder<double> builder, double value)
    {
        return new ComparisonAssertion<double>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<double> IsLessThanOrEqualTo(this ValueAssertionBuilder<double> builder, double value)
    {
        return new ComparisonAssertion<double>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<double> IsPositive(this ValueAssertionBuilder<double> builder)
    {
        return builder.IsGreaterThan(0.0);
    }

    public static ComparisonAssertion<double> IsNegative(this ValueAssertionBuilder<double> builder)
    {
        return builder.IsLessThan(0.0);
    }

    // === FLOAT EXTENSIONS ===
    public static ComparisonAssertion<float> IsGreaterThan(this ValueAssertionBuilder<float> builder, float value)
    {
        return new ComparisonAssertion<float>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<float> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<float> builder, float value)
    {
        return new ComparisonAssertion<float>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<float> IsLessThan(this ValueAssertionBuilder<float> builder, float value)
    {
        return new ComparisonAssertion<float>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<float> IsLessThanOrEqualTo(this ValueAssertionBuilder<float> builder, float value)
    {
        return new ComparisonAssertion<float>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<float> IsPositive(this ValueAssertionBuilder<float> builder)
    {
        return builder.IsGreaterThan(0.0f);
    }

    public static ComparisonAssertion<float> IsNegative(this ValueAssertionBuilder<float> builder)
    {
        return builder.IsLessThan(0.0f);
    }

    // === DECIMAL EXTENSIONS ===
    public static ComparisonAssertion<decimal> IsGreaterThan(this ValueAssertionBuilder<decimal> builder, decimal value)
    {
        return new ComparisonAssertion<decimal>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<decimal> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<decimal> builder, decimal value)
    {
        return new ComparisonAssertion<decimal>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<decimal> IsLessThan(this ValueAssertionBuilder<decimal> builder, decimal value)
    {
        return new ComparisonAssertion<decimal>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<decimal> IsLessThanOrEqualTo(this ValueAssertionBuilder<decimal> builder, decimal value)
    {
        return new ComparisonAssertion<decimal>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<decimal> IsPositive(this ValueAssertionBuilder<decimal> builder)
    {
        return builder.IsGreaterThan(0m);
    }

    public static ComparisonAssertion<decimal> IsNegative(this ValueAssertionBuilder<decimal> builder)
    {
        return builder.IsLessThan(0m);
    }

    // === SHORT EXTENSIONS ===
    public static ComparisonAssertion<short> IsGreaterThan(this ValueAssertionBuilder<short> builder, short value)
    {
        return new ComparisonAssertion<short>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<short> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<short> builder, short value)
    {
        return new ComparisonAssertion<short>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<short> IsLessThan(this ValueAssertionBuilder<short> builder, short value)
    {
        return new ComparisonAssertion<short>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<short> IsLessThanOrEqualTo(this ValueAssertionBuilder<short> builder, short value)
    {
        return new ComparisonAssertion<short>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<short> IsPositive(this ValueAssertionBuilder<short> builder)
    {
        return builder.IsGreaterThan((short)0);
    }

    public static ComparisonAssertion<short> IsNegative(this ValueAssertionBuilder<short> builder)
    {
        return builder.IsLessThan((short)0);
    }

    // === BYTE EXTENSIONS ===
    public static ComparisonAssertion<byte> IsGreaterThan(this ValueAssertionBuilder<byte> builder, byte value)
    {
        return new ComparisonAssertion<byte>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<byte> IsGreaterThanOrEqualTo(this ValueAssertionBuilder<byte> builder, byte value)
    {
        return new ComparisonAssertion<byte>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<byte> IsLessThan(this ValueAssertionBuilder<byte> builder, byte value)
    {
        return new ComparisonAssertion<byte>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<byte> IsLessThanOrEqualTo(this ValueAssertionBuilder<byte> builder, byte value)
    {
        return new ComparisonAssertion<byte>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static ComparisonAssertion<byte> IsPositive(this ValueAssertionBuilder<byte> builder)
    {
        return builder.IsGreaterThan((byte)0);
    }
}