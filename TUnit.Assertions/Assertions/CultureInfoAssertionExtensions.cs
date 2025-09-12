using System.Globalization;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(CultureInfo), nameof(CultureInfo.IsNeutralCulture))]
[CreateAssertion(typeof(CultureInfo), nameof(CultureInfo.IsNeutralCulture), CustomName = "IsNotNeutralCulture", NegateLogic = true)]

[CreateAssertion(typeof(CultureInfo), nameof(CultureInfo.IsReadOnly))]
[CreateAssertion(typeof(CultureInfo), nameof(CultureInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]

// Custom helper methods
[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsInvariant))]
[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsInvariant), CustomName = "IsNotInvariant", NegateLogic = true)]

[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsEnglish))]
[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsEnglish), CustomName = "IsNotEnglish", NegateLogic = true)]

[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsRightToLeft))]
[CreateAssertion(typeof(CultureInfo), typeof(CultureInfoAssertionExtensions), nameof(IsRightToLeft), CustomName = "IsLeftToRight", NegateLogic = true)]
public static partial class CultureInfoAssertionExtensions
{
    internal static bool IsInvariant(CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture);
    internal static bool IsEnglish(CultureInfo culture) => culture.TwoLetterISOLanguageName == "en";
    internal static bool IsRightToLeft(CultureInfo culture) => culture.TextInfo.IsRightToLeft;
}