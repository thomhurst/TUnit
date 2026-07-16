using System.Net;
using System.Net.Http;
using System.Text;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class HttpResponseMessageAssertionTests
{
    // Specific status code assertions

    [Test]
    public async Task Test_HttpResponseMessage_IsOk()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        await Assert.That(response).IsOk();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsCreated()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Created);
        await Assert.That(response).IsCreated();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsNoContent()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await Assert.That(response).IsNoContent();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsBadRequest()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        await Assert.That(response).IsBadRequest();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsUnauthorized()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        await Assert.That(response).IsUnauthorized();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsForbidden()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        await Assert.That(response).IsForbidden();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsNotFound()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        await Assert.That(response).IsNotFound();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsConflict()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict);
        await Assert.That(response).IsConflict();
    }

    // Parameterized status code assertion

    [Test]
    public async Task Test_HttpResponseMessage_HasStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Accepted);
        await Assert.That(response).HasStatusCode(HttpStatusCode.Accepted);
    }

    // Range check assertions

    [Test]
    public async Task Test_HttpResponseMessage_IsRedirectStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.MovedPermanently);
        await Assert.That(response).IsRedirectStatusCode();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsClientErrorStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        await Assert.That(response).IsClientErrorStatusCode();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsServerErrorStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await Assert.That(response).IsServerErrorStatusCode();
    }

    // Content assertions

    [Test]
    public async Task Test_HttpResponseMessage_HasJsonContent()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        await Assert.That(response).HasJsonContent();
    }

    [Test]
    public async Task Test_HttpResponseMessage_HasContentType()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html></html>", Encoding.UTF8, "text/html")
        };
        await Assert.That(response).HasContentType("text/html");
    }

    // Header assertion

    [Test]
    public async Task Test_HttpResponseMessage_HasHeader()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Custom-Header", "value");
        await Assert.That(response).HasHeader("X-Custom-Header");
    }

    [Test]
    public async Task Test_HttpResponseMessage_HasHeader_ContentHeader()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        await Assert.That(response).HasHeader("Content-Type");
    }

    // Existing assertions still work

    [Test]
    public async Task Test_HttpResponseMessage_IsSuccessStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        await Assert.That(response).IsSuccessStatusCode();
    }

    [Test]
    public async Task Test_HttpResponseMessage_IsNotSuccessStatusCode()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        await Assert.That(response).IsNotSuccessStatusCode();
    }
}
