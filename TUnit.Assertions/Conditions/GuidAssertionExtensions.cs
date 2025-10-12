using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Guid type using [GenerateAssertion] attributes.
/// </summary>
public static partial class GuidAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be an empty GUID")]
    public static bool IsEmptyGuid(this Guid value) => value == Guid.Empty;

    [GenerateAssertion(ExpectationMessage = "to not be an empty GUID")]
    public static bool IsNotEmptyGuid(this Guid value) => value != Guid.Empty;
}
