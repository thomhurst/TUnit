using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with IComparable constraint
/// Should generate assertion class and extension method preserving the constraint
/// </summary>
public static partial class ComparableConstraintExtensions
{
    [GenerateAssertion(ExpectationMessage = "be greater than {0}")]
    public static bool IsGreaterThan<T>(this int value, T other) where T : IComparable<T>
    {
        return other.CompareTo(default(T)) > 0;
    }

    [GenerateAssertion(ExpectationMessage = "be between {0} and {1}")]
    public static bool IsBetween<T>(this int value, T min, T max) where T : IComparable<T>
    {
        return min.CompareTo(max) <= 0;
    }
}
