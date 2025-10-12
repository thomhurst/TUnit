using System.Net;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for HttpStatusCode type using [GenerateAssertion] attributes.
/// These wrap HTTP status code range checks as extension methods.
/// </summary>
public static class HttpStatusCodeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be a success status code (2xx)")]
    public static bool IsSuccess(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 200 && code < 300;
    }

    [GenerateAssertion(ExpectationMessage = "to not be a success status code")]
    public static bool IsNotSuccess(this HttpStatusCode value)
    {
        var code = (int)value;
        return code < 200 || code >= 300;
    }

    [GenerateAssertion(ExpectationMessage = "to be a client error status code (4xx)")]
    public static bool IsClientError(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 400 && code < 500;
    }

    [GenerateAssertion(ExpectationMessage = "to be a server error status code (5xx)")]
    public static bool IsServerError(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 500 && code < 600;
    }

    [GenerateAssertion(ExpectationMessage = "to be a redirection status code (3xx)")]
    public static bool IsRedirection(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 300 && code < 400;
    }

    [GenerateAssertion(ExpectationMessage = "to be an informational status code (1xx)")]
    public static bool IsInformational(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 100 && code < 200;
    }

    [GenerateAssertion(ExpectationMessage = "to be an error status code (4xx or 5xx)")]
    public static bool IsError(this HttpStatusCode value)
    {
        var code = (int)value;
        return code >= 400 && code < 600;
    }
}
