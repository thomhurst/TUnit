using System.Net;
using System.Net.Http;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for HttpResponseMessage type.
/// Provides fluent assertions for HTTP status codes, content types, and headers.
/// </summary>
[AssertionFrom<HttpResponseMessage>(nameof(HttpResponseMessage.IsSuccessStatusCode), ExpectationMessage = "have a success status code")]
[AssertionFrom<HttpResponseMessage>(nameof(HttpResponseMessage.IsSuccessStatusCode), CustomName = "IsNotSuccessStatusCode", NegateLogic = true, ExpectationMessage = "have a success status code")]
public static partial class HttpResponseMessageAssertionExtensions
{
    // Specific status code assertions

    [GenerateAssertion(ExpectationMessage = "have status code 200 OK", InlineMethodBody = true)]
    public static bool IsOk(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.OK;

    [GenerateAssertion(ExpectationMessage = "have status code 201 Created", InlineMethodBody = true)]
    public static bool IsCreated(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Created;

    [GenerateAssertion(ExpectationMessage = "have status code 204 No Content", InlineMethodBody = true)]
    public static bool IsNoContent(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.NoContent;

    [GenerateAssertion(ExpectationMessage = "have status code 400 Bad Request", InlineMethodBody = true)]
    public static bool IsBadRequest(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.BadRequest;

    [GenerateAssertion(ExpectationMessage = "have status code 401 Unauthorized", InlineMethodBody = true)]
    public static bool IsUnauthorized(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Unauthorized;

    [GenerateAssertion(ExpectationMessage = "have status code 403 Forbidden", InlineMethodBody = true)]
    public static bool IsForbidden(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Forbidden;

    [GenerateAssertion(ExpectationMessage = "have status code 404 Not Found", InlineMethodBody = true)]
    public static bool IsNotFound(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.NotFound;

    [GenerateAssertion(ExpectationMessage = "have status code 409 Conflict", InlineMethodBody = true)]
    public static bool IsConflict(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Conflict;

    // Parameterized status code assertion

    [GenerateAssertion(ExpectationMessage = "have status code {statusCode}", InlineMethodBody = true)]
    public static bool HasStatusCode(this HttpResponseMessage response, HttpStatusCode statusCode)
        => response.StatusCode == statusCode;

    // Range check assertions

    [GenerateAssertion(ExpectationMessage = "have a redirection status code (3xx)", InlineMethodBody = true)]
    public static bool IsRedirectStatusCode(this HttpResponseMessage response)
        => (int)response.StatusCode is >= 300 and < 400;

    [GenerateAssertion(ExpectationMessage = "have a client error status code (4xx)", InlineMethodBody = true)]
    public static bool IsClientErrorStatusCode(this HttpResponseMessage response)
        => (int)response.StatusCode is >= 400 and < 500;

    [GenerateAssertion(ExpectationMessage = "have a server error status code (5xx)", InlineMethodBody = true)]
    public static bool IsServerErrorStatusCode(this HttpResponseMessage response)
        => (int)response.StatusCode is >= 500 and < 600;

    // Content assertions

    [GenerateAssertion(ExpectationMessage = "have JSON content type", InlineMethodBody = true)]
    public static bool HasJsonContent(this HttpResponseMessage response)
        => response.Content.Headers.ContentType?.MediaType == "application/json";

    [GenerateAssertion(ExpectationMessage = "have content type {contentType}", InlineMethodBody = true)]
    public static bool HasContentType(this HttpResponseMessage response, string contentType)
        => response.Content.Headers.ContentType?.MediaType == contentType;

    // Header assertion

    [GenerateAssertion(ExpectationMessage = "have header '{headerName}'")]
    public static bool HasHeader(this HttpResponseMessage response, string headerName)
    {
        try
        {
            if (response.Headers.Contains(headerName))
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Header name is a known content header - check content headers instead
        }

        try
        {
            if (response.Content?.Headers.Contains(headerName) ?? false)
            {
                return true;
            }
        }
        catch (InvalidOperationException)
        {
            // Header name is a known response header - already checked above
        }

        return false;
    }
}
