using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Encoding type using [AssertionFrom&lt;Encoding&gt;] attributes.
/// Each assertion wraps a property from the Encoding class.
/// </summary>
[AssertionFrom<Encoding>("IsSingleByte", ExpectationMessage = "be single-byte encoding")]
[AssertionFrom<Encoding>("IsSingleByte", CustomName = "IsNotSingleByte", NegateLogic = true, ExpectationMessage = "be single-byte encoding")]
public static partial class EncodingPropertyAssertions
{
}
