using System.Net.Http;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for HttpResponseMessage type using [AssertionFrom&lt;HttpResponseMessage&gt;] attributes.
/// These wrap HTTP response validation checks as extension methods.
/// </summary>
[AssertionFrom<HttpResponseMessage>(nameof(HttpResponseMessage.IsSuccessStatusCode), ExpectationMessage = "have a success status code")]
[AssertionFrom<HttpResponseMessage>(nameof(HttpResponseMessage.IsSuccessStatusCode), CustomName = "IsNotSuccessStatusCode", NegateLogic = true, ExpectationMessage = "have a success status code")]
public static partial class HttpResponseMessageAssertionExtensions
{
}
