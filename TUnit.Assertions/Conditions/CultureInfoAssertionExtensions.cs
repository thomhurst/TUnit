using System.Globalization;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CultureInfo type using [GenerateAssertion] attributes.
/// These wrap culture equality and property checks as extension methods.
/// </summary>
public static class CultureInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be invariant culture")]
    public static bool IsInvariant(this CultureInfo value) => value?.Equals(CultureInfo.InvariantCulture) == true;

    [GenerateAssertion(ExpectationMessage = "to not be invariant culture")]
    public static bool IsNotInvariant(this CultureInfo value) => !(value?.Equals(CultureInfo.InvariantCulture) == true);

    [GenerateAssertion(ExpectationMessage = "to be English culture")]
    public static bool IsEnglish(this CultureInfo value) => value?.TwoLetterISOLanguageName == "en";

    [GenerateAssertion(ExpectationMessage = "to not be English culture")]
    public static bool IsNotEnglish(this CultureInfo value) => value?.TwoLetterISOLanguageName != "en";

    [GenerateAssertion(ExpectationMessage = "to be right-to-left culture")]
    public static bool IsRightToLeft(this CultureInfo value) => value?.TextInfo.IsRightToLeft == true;

    [GenerateAssertion(ExpectationMessage = "to be left-to-right culture")]
    public static bool IsLeftToRight(this CultureInfo value) => value?.TextInfo.IsRightToLeft == false;
}
