using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with multiple constraints
/// Should generate assertion preserving all constraints
/// </summary>
public static partial class MultipleConstraintsExtensions
{
    [GenerateAssertion(ExpectationMessage = "have the property")]
    public static bool HasProperty<T>(this string obj, T value)
        where T : class, IComparable<T>, new()
    {
        // Simplified implementation - just checks if value is not null
        return value != null;
    }
}
