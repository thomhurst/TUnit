using System.Net;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Cookie type using [AssertionFrom&lt;Cookie&gt;] attributes.
/// Each assertion wraps a property from the Cookie class for web and authentication testing.
/// </summary>
[AssertionFrom<Cookie>(nameof(Cookie.HttpOnly), ExpectationMessage = "be HTTP-only")]
[AssertionFrom<Cookie>(nameof(Cookie.HttpOnly), CustomName = "IsNotHttpOnly", NegateLogic = true, ExpectationMessage = "be HTTP-only")]

[AssertionFrom<Cookie>(nameof(Cookie.Secure), ExpectationMessage = "be secure")]
[AssertionFrom<Cookie>(nameof(Cookie.Secure), CustomName = "IsNotSecure", NegateLogic = true, ExpectationMessage = "be secure")]

[AssertionFrom<Cookie>(nameof(Cookie.Expired), ExpectationMessage = "be expired")]
[AssertionFrom<Cookie>(nameof(Cookie.Expired), CustomName = "IsNotExpired", NegateLogic = true, ExpectationMessage = "be expired")]
public static partial class CookieAssertionExtensions
{
}
