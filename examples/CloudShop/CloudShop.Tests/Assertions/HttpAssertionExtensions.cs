using System.Net;
using System.Net.Http;
using TUnit.Assertions.Attributes;

namespace CloudShop.Tests.Assertions;

/// <summary>
/// Custom HTTP assertion extensions for CloudShop API testing.
/// These complement the built-in TUnit HttpResponseMessage assertions.
/// </summary>
public static partial class HttpAssertionExtensions
{
    /// <summary>
    /// Asserts that the HTTP response has a JSON content type.
    /// Usage: await Assert.That(response).HasJsonContent();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have JSON content type")]
    public static bool HasJsonContent(this HttpResponseMessage response)
        => response.Content.Headers.ContentType?.MediaType == "application/json";

    /// <summary>
    /// Asserts that the HTTP response has the expected status code.
    /// Usage: await Assert.That(response).HasStatusCode(HttpStatusCode.Created);
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code {expected}")]
    public static bool HasStatusCode(this HttpResponseMessage response, HttpStatusCode expected)
        => response.StatusCode == expected;

    /// <summary>
    /// Asserts that the HTTP response has a 201 Created status code.
    /// Usage: await Assert.That(response).IsCreated();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code 201 Created")]
    public static bool IsCreated(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Created;

    /// <summary>
    /// Asserts that the HTTP response has a 404 Not Found status code.
    /// Usage: await Assert.That(response).IsNotFound();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code 404 Not Found")]
    public static bool IsNotFound(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.NotFound;

    /// <summary>
    /// Asserts that the HTTP response has a 400 Bad Request status code.
    /// Usage: await Assert.That(response).IsBadRequest();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code 400 Bad Request")]
    public static bool IsBadRequest(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.BadRequest;

    /// <summary>
    /// Asserts that the HTTP response has a 401 Unauthorized status code.
    /// Usage: await Assert.That(response).IsUnauthorized();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code 401 Unauthorized")]
    public static bool IsUnauthorized(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Unauthorized;

    /// <summary>
    /// Asserts that the HTTP response has a 403 Forbidden status code.
    /// Usage: await Assert.That(response).IsForbidden();
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "have status code 403 Forbidden")]
    public static bool IsForbidden(this HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Forbidden;
}
