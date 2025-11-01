using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for StringBuilder type using [GenerateAssertion(InlineMethodBody = true)] attributes.
/// These wrap StringBuilder property checks as extension methods.
/// </summary>
file static partial class StringBuilderAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be empty", InlineMethodBody = true)]
    public static bool IsEmpty(this StringBuilder value) => value?.Length == 0;
    [GenerateAssertion(ExpectationMessage = "to not be empty", InlineMethodBody = true)]
    public static bool IsNotEmpty(this StringBuilder value) => value?.Length > 0;
    [GenerateAssertion(ExpectationMessage = "to have excess capacity", InlineMethodBody = true)]
    public static bool HasExcessCapacity(this StringBuilder value) => value != null && value.Capacity > value.Length;
}
