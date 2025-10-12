using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for StringBuilder type using [GenerateAssertion] attributes.
/// These wrap StringBuilder property checks as extension methods.
/// </summary>
public static partial class StringBuilderAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static bool IsEmpty(this StringBuilder value) => value?.Length == 0;

    [GenerateAssertion(ExpectationMessage = "to not be empty")]
    public static bool IsNotEmpty(this StringBuilder value) => value?.Length > 0;

    [GenerateAssertion(ExpectationMessage = "to have excess capacity")]
    public static bool HasExcessCapacity(this StringBuilder value) => value != null && value.Capacity > value.Length;
}
