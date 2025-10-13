#if NET6_0_OR_GREATER
using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Range type using [GenerateAssertion] attributes.
/// These wrap range checks as extension methods.
/// </summary>
public static partial class RangeAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have both indices from the end")]
    public static bool HasBothIndicesFromEnd(this Range value) => value.Start.IsFromEnd && value.End.IsFromEnd;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have start index from beginning")]
    public static bool HasStartFromBeginning(this Range value) => !value.Start.IsFromEnd;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to have end index from beginning")]
    public static bool HasEndFromBeginning(this Range value) => !value.End.IsFromEnd;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be the all range")]
    public static bool IsAll(this Range value) => value.Equals(Range.All);
}
#endif
