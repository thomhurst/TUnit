using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for IComparable type assertions
/// </summary>
public static class ComparableAssertionExtensions
{
    // === For ValueAssertionBuilder<T> where T : IComparable<T> ===
    public static ComparisonAssertion<T> IsGreaterThan<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<T> IsGreaterThanOrEqualTo<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<T> IsLessThan<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<T> IsLessThanOrEqualTo<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static CustomAssertion<T> IsBetween<T>(this ValueAssertionBuilder<T> builder, T min, T max)
        where T : IComparable<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => actual != null && actual.CompareTo(min) >= 0 && actual.CompareTo(max) <= 0,
            $"Expected value to be between {min} and {max} but was {{ActualValue}}");
    }

    // === For DualAssertionBuilder<T> where T : IComparable<T> ===
    public static ComparisonAssertion<T> IsGreaterThan<T>(this DualAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThan);
    }

    public static ComparisonAssertion<T> IsGreaterThanOrEqualTo<T>(this DualAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    public static ComparisonAssertion<T> IsLessThan<T>(this DualAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThan);
    }

    public static ComparisonAssertion<T> IsLessThanOrEqualTo<T>(this DualAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return new ComparisonAssertion<T>(builder.ActualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    public static CustomAssertion<T> IsBetween<T>(this DualAssertionBuilder<T> builder, T min, T max)
        where T : IComparable<T>
    {
        return new CustomAssertion<T>(builder.ActualValueProvider,
            actual => actual != null && actual.CompareTo(min) >= 0 && actual.CompareTo(max) <= 0,
            $"Expected value to be between {min} and {max} but was {{ActualValue}}");
    }

    // === Aliases for backwards compatibility ===
    public static ComparisonAssertion<T> GreaterThanOrEqualTo<T>(this ValueAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return builder.IsGreaterThanOrEqualTo(value);
    }

    public static ComparisonAssertion<T> GreaterThanOrEqualTo<T>(this DualAssertionBuilder<T> builder, T value)
        where T : IComparable<T>
    {
        return builder.IsGreaterThanOrEqualTo(value);
    }
}