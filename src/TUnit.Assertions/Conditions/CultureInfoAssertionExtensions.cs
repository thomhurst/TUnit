using System.Globalization;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CultureInfo type using [GenerateAssertion(InlineMethodBody = true)] and [AssertionFrom&lt;CultureInfo&gt;] attributes.
/// These wrap culture equality, property checks, and instance properties as extension methods.
/// </summary>
[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsNeutralCulture), ExpectationMessage = "be a neutral culture")]
[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsNeutralCulture), CustomName = "IsNotNeutralCulture", NegateLogic = true, ExpectationMessage = "be a neutral culture")]

[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsReadOnly), ExpectationMessage = "be read-only culture")]
file static partial class CultureInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be invariant culture", InlineMethodBody = true)]
    public static bool IsInvariant(this CultureInfo value) => value?.Equals(CultureInfo.InvariantCulture) == true;
    [GenerateAssertion(ExpectationMessage = "to not be invariant culture", InlineMethodBody = true)]
    public static bool IsNotInvariant(this CultureInfo value) => !(value?.Equals(CultureInfo.InvariantCulture) == true);
    [GenerateAssertion(ExpectationMessage = "to be English culture", InlineMethodBody = true)]
    public static bool IsEnglish(this CultureInfo value) => value?.TwoLetterISOLanguageName == "en";
    [GenerateAssertion(ExpectationMessage = "to not be English culture", InlineMethodBody = true)]
    public static bool IsNotEnglish(this CultureInfo value) => value?.TwoLetterISOLanguageName != "en";
    [GenerateAssertion(ExpectationMessage = "to be right-to-left culture", InlineMethodBody = true)]
    public static bool IsRightToLeft(this CultureInfo value) => value?.TextInfo.IsRightToLeft == true;
    [GenerateAssertion(ExpectationMessage = "to be left-to-right culture", InlineMethodBody = true)]
    public static bool IsLeftToRight(this CultureInfo value) => value?.TextInfo.IsRightToLeft == false;
}
