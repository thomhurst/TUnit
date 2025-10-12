using System.Globalization;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CultureInfo type using [AssertionFrom&lt;CultureInfo&gt;] attributes.
/// Each assertion wraps a property from the CultureInfo class.
/// </summary>
[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsNeutralCulture), ExpectationMessage = "be a neutral culture")]
[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsNeutralCulture), CustomName = "IsNotNeutralCulture", NegateLogic = true, ExpectationMessage = "be a neutral culture")]

[AssertionFrom<CultureInfo>(nameof(CultureInfo.IsReadOnly), ExpectationMessage = "be read-only culture")]
public static partial class CultureInfoPropertyAssertions
{
}
