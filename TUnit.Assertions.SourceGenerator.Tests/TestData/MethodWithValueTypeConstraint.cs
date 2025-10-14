using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with value type constraint (struct)
/// Should generate assertion preserving the struct constraint
/// </summary>
public static partial class ValueTypeConstraintExtensions
{
    [GenerateAssertion(ExpectationMessage = "be the default value")]
    public static bool IsDefault<T>(this int value, T obj) where T : struct
    {
        return EqualityComparer<T>.Default.Equals(obj, default(T));
    }
}
