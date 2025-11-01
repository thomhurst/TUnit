#if NET6_0_OR_GREATER
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Range type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap range checks as extension methods.
/// </summary>
file static partial class RangeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have both indices from the end", InlineMethodBody = true)]
    public static bool HasBothIndicesFromEnd(this Range value) => value.Start.IsFromEnd && value.End.IsFromEnd;
    [GenerateAssertion(ExpectationMessage = "to have start index from beginning", InlineMethodBody = true)]
    public static bool HasStartFromBeginning(this Range value) => !value.Start.IsFromEnd;
    [GenerateAssertion(ExpectationMessage = "to have end index from beginning", InlineMethodBody = true)]
    public static bool HasEndFromBeginning(this Range value) => !value.End.IsFromEnd;
    [GenerateAssertion(ExpectationMessage = "to be the all range", InlineMethodBody = true)]
    public static bool IsAll(this Range value) => value.Equals(Range.All);
}
#endif
