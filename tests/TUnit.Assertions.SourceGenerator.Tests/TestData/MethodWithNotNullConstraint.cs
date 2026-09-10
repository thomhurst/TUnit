using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with notnull constraint
/// Should generate assertion preserving the notnull constraint
/// </summary>
public static partial class NotNullConstraintExtensions
{
    [GenerateAssertion(ExpectationMessage = "have a non-null value")]
    public static bool HasValue<T>(this string str, T value) where T : notnull
    {
        return value != null;
    }
}
