using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Uri type using [AssertionFrom&lt;Uri&gt;] attributes.
/// Each assertion wraps a property from the Uri class.
/// </summary>
[AssertionFrom<Uri>("IsAbsoluteUri", ExpectationMessage = "be an absolute URI")]
[AssertionFrom<Uri>("IsAbsoluteUri", CustomName = "IsNotAbsoluteUri", NegateLogic = true, ExpectationMessage = "be an absolute URI")]

[AssertionFrom<Uri>("IsFile", ExpectationMessage = "be a file URI")]
[AssertionFrom<Uri>("IsFile", CustomName = "IsNotFile", NegateLogic = true, ExpectationMessage = "be a file URI")]

[AssertionFrom<Uri>("IsUnc", ExpectationMessage = "be a UNC URI")]
[AssertionFrom<Uri>("IsUnc", CustomName = "IsNotUnc", NegateLogic = true, ExpectationMessage = "be a UNC URI")]

[AssertionFrom<Uri>("IsLoopback", ExpectationMessage = "be a loopback URI")]
[AssertionFrom<Uri>("IsLoopback", CustomName = "IsNotLoopback", NegateLogic = true, ExpectationMessage = "be a loopback URI")]

[AssertionFrom<Uri>("IsDefaultPort", ExpectationMessage = "use the default port")]
[AssertionFrom<Uri>("IsDefaultPort", CustomName = "IsNotDefaultPort", NegateLogic = true, ExpectationMessage = "use the default port")]

[AssertionFrom<Uri>("UserEscaped", ExpectationMessage = "be user-escaped")]
[AssertionFrom<Uri>("UserEscaped", CustomName = "IsNotUserEscaped", NegateLogic = true, ExpectationMessage = "be user-escaped")]
public static partial class UriAssertionExtensions
{
}
