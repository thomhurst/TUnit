using System.Globalization;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<CultureInfo>( nameof(CultureInfo.IsNeutralCulture))]
[CreateAssertion<CultureInfo>( nameof(CultureInfo.IsNeutralCulture), CustomName = "IsNotNeutralCulture", NegateLogic = true)]

[CreateAssertion<CultureInfo>( nameof(CultureInfo.IsReadOnly))]
[CreateAssertion<CultureInfo>( nameof(CultureInfo.IsReadOnly), CustomName = "IsNotReadOnly", NegateLogic = true)]

// Custom helper methods
[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsInvariant))]
[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsInvariant), CustomName = "IsNotInvariant", NegateLogic = true)]

[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsEnglish))]
[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsEnglish), CustomName = "IsNotEnglish", NegateLogic = true)]

[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsRightToLeft))]
[CreateAssertion<CultureInfo>( typeof(CultureInfoAssertionExtensions), nameof(IsRightToLeft), CustomName = "IsLeftToRight", NegateLogic = true)]
public static partial class CultureInfoAssertionExtensions
{
    internal static bool IsInvariant(CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture);
    internal static bool IsEnglish(CultureInfo culture) => culture.TwoLetterISOLanguageName == "en";
    internal static bool IsRightToLeft(CultureInfo culture) => culture.TextInfo.IsRightToLeft;
}