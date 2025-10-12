using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CancellationToken type using [GenerateAssertion] attributes.
/// These wrap CancellationToken.None equality checks as extension methods.
/// </summary>
public static partial class CancellationTokenAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be CancellationToken.None")]
    public static bool IsNone(this CancellationToken value) => value.Equals(CancellationToken.None);

    [GenerateAssertion(ExpectationMessage = "to not be CancellationToken.None")]
    public static bool IsNotNone(this CancellationToken value) => !value.Equals(CancellationToken.None);
}
