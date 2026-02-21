using System.Net;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Http;

namespace TUnit.Mocks.Http.Tests;

public class MockHttpHandlerTests
{
    [Test]
    public async Task ReturnsConfiguredResponse()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/users").RespondWithJson("""[{"id":1}]""");

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/users");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("id");
    }

    [Test]
    public async Task ReturnsDefaultStatusForUnmatched()
    {
        var handler = new MockHttpHandler();

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/unknown");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task WithDefaultStatusOverridesDefault()
    {
        var handler = new MockHttpHandler()
            .WithDefaultStatus(HttpStatusCode.ServiceUnavailable);

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/anything");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task ThrowOnUnmatchedThrowsException()
    {
        var handler = new MockHttpHandler().ThrowOnUnmatched();

        using var client = handler.CreateClient("http://localhost");

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/nothing"));
    }

    [Test]
    public async Task MatchesByMethod()
    {
        var handler = new MockHttpHandler();
        handler.OnPost("/api/data").Respond(HttpStatusCode.Created);

        using var client = handler.CreateClient("http://localhost");
        var getResponse = await client.GetAsync("/api/data");
        var postResponse = await client.PostAsync("/api/data", null);

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(postResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task MatchesByPath()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/v1/users").Respond(HttpStatusCode.OK);
        handler.OnGet("/api/v1/orders").Respond(HttpStatusCode.Accepted);

        using var client = handler.CreateClient("http://localhost");
        var usersResponse = await client.GetAsync("/api/v1/users");
        var ordersResponse = await client.GetAsync("/api/v1/orders");

        await Assert.That(usersResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ordersResponse.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
    }

    [Test]
    public async Task MatchesByHeader()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.Method(HttpMethod.Get)
            .Path("/api/secure")
            .Header("Authorization", "Bearer token123"))
            .Respond(HttpStatusCode.OK);

        using var client = handler.CreateClient("http://localhost");

        // Without auth header → default 404
        var noAuthResponse = await client.GetAsync("/api/secure");
        await Assert.That(noAuthResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        // With auth header → 200
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/secure");
        request.Headers.Add("Authorization", "Bearer token123");
        var authResponse = await client.SendAsync(request);
        await Assert.That(authResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MatchesByPathPrefix()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.Method(HttpMethod.Get).PathStartsWith("/api/v2"))
            .Respond(HttpStatusCode.OK);

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/v2/anything/here");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MatchesByBodyContent()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.Method(HttpMethod.Post).Path("/api/search").BodyContains("query"))
            .RespondWithJson("""{"results": []}""");

        using var client = handler.CreateClient("http://localhost");
        var content = new StringContent("""{"query": "test"}""", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/search", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MatchesByRegexPattern()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.Method(HttpMethod.Get).PathMatches(@"/api/users/\d+"))
            .RespondWithJson("""{"id": 1, "name": "Test"}""");

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/users/42");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OnAnyRequestMatchesEverything()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond(HttpStatusCode.OK);

        using var client = handler.CreateClient("http://localhost");
        var get = await client.GetAsync("/anything");
        var post = await client.PostAsync("/whatever", null);

        await Assert.That(get.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(post.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task StringContentResponse()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/hello").RespondWithString("Hello, World!");

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/hello");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task ResponseWithCustomHeaders()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/data")
            .Respond()
            .WithHeader("X-Custom", "value123");

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/data");

        await Assert.That(response.Headers.Contains("X-Custom")).IsTrue();
    }

    [Test]
    public async Task ResponseWithCustomStatusCode()
    {
        var handler = new MockHttpHandler();
        handler.OnDelete("/api/item/1").Respond(HttpStatusCode.NoContent);

        using var client = handler.CreateClient("http://localhost");
        var response = await client.DeleteAsync("/api/item/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SetupThrowsException()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/fail").Throws(new HttpRequestException("Connection refused"));

        using var client = handler.CreateClient("http://localhost");

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/fail"));
    }

    [Test]
    public async Task SetupThrowsWithMessage()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/timeout").Throws("Request timed out");

        using var client = handler.CreateClient("http://localhost");

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/timeout"));
    }

    [Test]
    public async Task SequentialResponses()
    {
        var handler = new MockHttpHandler();
        var setup = handler.OnGet("/api/counter");
        setup.RespondWithString("1");
        setup.Then().RespondWithString("2");
        setup.Then().RespondWithString("3");

        using var client = handler.CreateClient("http://localhost");
        var r1 = await client.GetStringAsync("/api/counter");
        var r2 = await client.GetStringAsync("/api/counter");
        var r3 = await client.GetStringAsync("/api/counter");
        // 4th call should repeat last response
        var r4 = await client.GetStringAsync("/api/counter");

        await Assert.That(r1).IsEqualTo("1");
        await Assert.That(r2).IsEqualTo("2");
        await Assert.That(r3).IsEqualTo("3");
        await Assert.That(r4).IsEqualTo("3");
    }

    [Test]
    public async Task CapturesRequests()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/users");
        await client.PostAsync("/api/users", new StringContent("{}"));

        await Assert.That(handler.Requests).Count().IsEqualTo(2);
        await Assert.That(handler.Requests[0].Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.Requests[1].Method).IsEqualTo(HttpMethod.Post);
    }

    [Test]
    public async Task CapturesRequestBody()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.PostAsync("/api/data",
            new StringContent("""{"name":"test"}""", System.Text.Encoding.UTF8, "application/json"));

        await Assert.That(handler.Requests[0].Body).Contains("test");
    }

    [Test]
    public async Task CapturesRequestHeaders()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/data");
        request.Headers.Add("X-Correlation-Id", "abc-123");
        await client.SendAsync(request);

        await Assert.That(handler.Requests[0].Headers.ContainsKey("X-Correlation-Id")).IsTrue();
    }

    [Test]
    public async Task TracksUnmatchedRequests()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/known").Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/known");
        await client.GetAsync("/api/unknown");

        await Assert.That(handler.UnmatchedRequests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task VerifyCallCount()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/users");
        await client.GetAsync("/api/users");

        handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Exactly(2));
    }

    [Test]
    public async Task VerifyThrowsOnMismatch()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/users");

        Assert.Throws<MockVerificationException>(() =>
            handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Exactly(3)));
    }

    [Test]
    public async Task VerifyNoUnmatchedRequestsPasses()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/users");

        handler.VerifyNoUnmatchedRequests();
    }

    [Test]
    public async Task VerifyNoUnmatchedRequestsFails()
    {
        var handler = new MockHttpHandler();

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/unknown");

        Assert.Throws<MockVerificationException>(() =>
            handler.VerifyNoUnmatchedRequests());
    }

    [Test]
    public async Task ResetClearsSetupsAndRequests()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/data").RespondWithString("data");

        using var client = handler.CreateClient("http://localhost");
        await client.GetAsync("/api/data");

        handler.Reset();

        await Assert.That(handler.Requests).Count().IsEqualTo(0);

        // After reset, no setups so should get default
        var response = await client.GetAsync("/api/data");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateClientWithBaseAddress()
    {
        var handler = new MockHttpHandler();
        handler.OnGet("/api/test").Respond();

        using var client = handler.CreateClient("http://example.com");

        await Assert.That(client.BaseAddress).IsNotNull();
        await Assert.That(client.BaseAddress!.Host).IsEqualTo("example.com");
    }

    [Test]
    public async Task OnPutMatchesPutRequests()
    {
        var handler = new MockHttpHandler();
        handler.OnPut("/api/item/1").Respond(HttpStatusCode.NoContent);

        using var client = handler.CreateClient("http://localhost");
        var response = await client.PutAsync("/api/item/1", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task FactoryResponseUsesRequest()
    {
        var handler = new MockHttpHandler();
        handler.OnAnyRequest().Respond().WithFactory(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"Echo: {req.Method} {req.RequestUri}")
            });

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/echo");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).Contains("GET");
        await Assert.That(body).Contains("/api/echo");
    }

    [Test]
    public async Task HasHeaderMatchesPresence()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.HasHeader("X-Api-Key")).Respond(HttpStatusCode.OK);

        using var client = handler.CreateClient("http://localhost");

        // Without header → 404
        var noHeader = await client.GetAsync("/api/data");
        await Assert.That(noHeader.StatusCode).IsEqualTo(HttpStatusCode.NotFound);

        // With header → 200
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/data");
        request.Headers.Add("X-Api-Key", "any-value");
        var withHeader = await client.SendAsync(request);
        await Assert.That(withHeader.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task CustomPredicateMatching()
    {
        var handler = new MockHttpHandler();
        handler.OnRequest(r => r.Matching(req =>
            req.RequestUri?.Query.Contains("page=1") == true))
            .RespondWithJson("""{"page": 1}""");

        using var client = handler.CreateClient("http://localhost");
        var response = await client.GetAsync("/api/data?page=1&size=10");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
