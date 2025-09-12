using System.Net;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// HTTP Status Code assertions
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsSuccess))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsSuccess), CustomName = "IsNotSuccess", NegateLogic = true)]

[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsRedirection))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsRedirection), CustomName = "IsNotRedirection", NegateLogic = true)]

[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsClientError))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsClientError), CustomName = "IsNotClientError", NegateLogic = true)]

[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsServerError))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsServerError), CustomName = "IsNotServerError", NegateLogic = true)]

[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsInformational))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsInformational), CustomName = "IsNotInformational", NegateLogic = true)]

[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsError))]
[CreateAssertion(typeof(HttpStatusCode), typeof(HttpStatusCodeAssertionExtensions), nameof(IsError), CustomName = "IsNotError", NegateLogic = true)]
public static partial class HttpStatusCodeAssertionExtensions
{
    internal static bool IsSuccess(HttpStatusCode statusCode) => 
        (int)statusCode >= 200 && (int)statusCode < 300;
    
    internal static bool IsRedirection(HttpStatusCode statusCode) => 
        (int)statusCode >= 300 && (int)statusCode < 400;
    
    internal static bool IsClientError(HttpStatusCode statusCode) => 
        (int)statusCode >= 400 && (int)statusCode < 500;
    
    internal static bool IsServerError(HttpStatusCode statusCode) => 
        (int)statusCode >= 500 && (int)statusCode < 600;
    
    internal static bool IsInformational(HttpStatusCode statusCode) => 
        (int)statusCode >= 100 && (int)statusCode < 200;
    
    internal static bool IsError(HttpStatusCode statusCode) => 
        (int)statusCode >= 400;
}