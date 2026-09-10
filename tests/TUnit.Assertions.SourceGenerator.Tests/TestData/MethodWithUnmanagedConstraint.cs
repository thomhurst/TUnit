using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Method with unmanaged constraint.
/// Should generate 'where T : unmanaged' — not 'struct, unmanaged' (CS0449) and not a
/// downgraded 'struct', since Roslyn also sets HasValueTypeConstraint for 'unmanaged' (#6471).
/// </summary>
public static partial class UnmanagedConstraintExtensions
{
    [GenerateAssertion(ExpectationMessage = "be blittable default")]
    public static bool IsBlittableDefault<T>(this int value, T obj) where T : unmanaged
    {
        return EqualityComparer<T>.Default.Equals(obj, default(T));
    }
}
