using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with reference type constraint (class)
/// Should generate assertion preserving the class constraint
/// </summary>
public static partial class ReferenceTypeConstraintExtensions
{
    [GenerateAssertion(ExpectationMessage = "be null or default")]
    public static bool IsNullOrDefault<T>(this string value, T obj) where T : class
    {
        return obj == null;
    }
}
