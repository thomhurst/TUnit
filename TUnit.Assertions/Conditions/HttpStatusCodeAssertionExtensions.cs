using System.ComponentModel;
using System.Net;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for HttpStatusCode type using [GenerateAssertion] attributes.
/// These wrap HTTP status code range checks as extension methods.
/// </summary>
public static partial class HttpStatusCodeAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a success status code (2xx)")]
    public static bool IsSuccess(this HttpStatusCode value) => (int)value is >= 200 and < 300;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be a success status code")]
    public static bool IsNotSuccess(this HttpStatusCode value) => (int)value is < 200 or >= 300;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a client error status code (4xx)")]
    public static bool IsClientError(this HttpStatusCode value) => (int)value is >= 400 and < 500;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a server error status code (5xx)")]
    public static bool IsServerError(this HttpStatusCode value) => (int)value is >= 500 and < 600;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a redirection status code (3xx)")]
    public static bool IsRedirection(this HttpStatusCode value) => (int)value is >= 300 and < 400;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be an informational status code (1xx)")]
    public static bool IsInformational(this HttpStatusCode value) => (int)value is >= 100 and < 200;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be an error status code (4xx or 5xx)")]
    public static bool IsError(this HttpStatusCode value) => (int)value is >= 400 and < 600;
}
