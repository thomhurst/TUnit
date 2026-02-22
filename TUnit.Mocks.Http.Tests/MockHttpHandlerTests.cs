using System.Net;
using TUnit.Mocks;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Http;

namespace TUnit.Mocks.Http.Tests;

public class MockHttpHandlerTests
{
    [Test]
    public async Task ReturnsConfiguredResponse()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/users").RespondWithJson("""[{"id":1}]""");

        var response = await client.GetAsync("/api/users");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("id");
    }

    [Test]
    public async Task ReturnsDefaultStatusForUnmatched()
    {
        using var client = Mock.HttpClient("http://localhost");

        var response = await client.GetAsync("/api/unknown");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task WithDefaultStatusOverridesDefault()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.WithDefaultStatus(HttpStatusCode.ServiceUnavailable);

        var response = await client.GetAsync("/api/anything");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    [Test]
    public async Task ThrowOnUnmatchedThrowsException()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.ThrowOnUnmatched();

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/nothing"));
    }

    [Test]
    public async Task MatchesByMethod()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnPost("/api/data").Respond(HttpStatusCode.Created);

        var getResponse = await client.GetAsync("/api/data");
        var postResponse = await client.PostAsync("/api/data", null);

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(postResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task MatchesByPath()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/v1/users").Respond(HttpStatusCode.OK);
        client.Handler.OnGet("/api/v1/orders").Respond(HttpStatusCode.Accepted);

        var usersResponse = await client.GetAsync("/api/v1/users");
        var ordersResponse = await client.GetAsync("/api/v1/orders");

        await Assert.That(usersResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(ordersResponse.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
    }

    [Test]
    public async Task MatchesByHeader()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.Method(HttpMethod.Get)
            .Path("/api/secure")
            .Header("Authorization", "Bearer token123"))
            .Respond(HttpStatusCode.OK);

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
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.Method(HttpMethod.Get).PathStartsWith("/api/v2"))
            .Respond(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/v2/anything/here");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MatchesByBodyContent()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.Method(HttpMethod.Post).Path("/api/search").BodyContains("query"))
            .RespondWithJson("""{"results": []}""");

        var content = new StringContent("""{"query": "test"}""", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/search", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MatchesByRegexPattern()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.Method(HttpMethod.Get).PathMatches(@"/api/users/\d+"))
            .RespondWithJson("""{"id": 1, "name": "Test"}""");

        var response = await client.GetAsync("/api/users/42");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OnAnyRequestMatchesEverything()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond(HttpStatusCode.OK);

        var get = await client.GetAsync("/anything");
        var post = await client.PostAsync("/whatever", null);

        await Assert.That(get.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(post.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task StringContentResponse()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/hello").RespondWithString("Hello, World!");

        var response = await client.GetAsync("/api/hello");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task ResponseWithCustomHeaders()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/data")
            .Respond()
            .WithHeader("X-Custom", "value123");

        var response = await client.GetAsync("/api/data");

        await Assert.That(response.Headers.Contains("X-Custom")).IsTrue();
    }

    [Test]
    public async Task ResponseWithCustomStatusCode()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnDelete("/api/item/1").Respond(HttpStatusCode.NoContent);

        var response = await client.DeleteAsync("/api/item/1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SetupThrowsException()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/fail").Throws(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/fail"));
    }

    [Test]
    public async Task SetupThrowsWithMessage()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/timeout").Throws("Request timed out");

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await client.GetAsync("/api/timeout"));
    }

    [Test]
    public async Task SequentialResponses()
    {
        using var client = Mock.HttpClient("http://localhost");
        var setup = client.Handler.OnGet("/api/counter");
        setup.RespondWithString("1");
        setup.Then().RespondWithString("2");
        setup.Then().RespondWithString("3");

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
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        await client.GetAsync("/api/users");
        await client.PostAsync("/api/users", new StringContent("{}"));

        await Assert.That(client.Handler.Requests).Count().IsEqualTo(2);
        await Assert.That(client.Handler.Requests[0].Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(client.Handler.Requests[1].Method).IsEqualTo(HttpMethod.Post);
    }

    [Test]
    public async Task CapturesRequestBody()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        await client.PostAsync("/api/data",
            new StringContent("""{"name":"test"}""", System.Text.Encoding.UTF8, "application/json"));

        await Assert.That(client.Handler.Requests[0].Body).Contains("test");
    }

    [Test]
    public async Task CapturesRequestHeaders()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/data");
        request.Headers.Add("X-Correlation-Id", "abc-123");
        await client.SendAsync(request);

        await Assert.That(client.Handler.Requests[0].Headers.ContainsKey("X-Correlation-Id")).IsTrue();
    }

    [Test]
    public async Task TracksUnmatchedRequests()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/known").Respond();

        await client.GetAsync("/api/known");
        await client.GetAsync("/api/unknown");

        await Assert.That(client.Handler.UnmatchedRequests).Count().IsEqualTo(1);
    }

    [Test]
    public async Task VerifyCallCount()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        await client.GetAsync("/api/users");
        await client.GetAsync("/api/users");

        client.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Exactly(2));
    }

    [Test]
    public async Task VerifyThrowsOnMismatch()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        await client.GetAsync("/api/users");

        Assert.Throws<MockVerificationException>(() =>
            client.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Exactly(3)));
    }

    [Test]
    public async Task VerifyNoUnmatchedRequestsPasses()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        await client.GetAsync("/api/users");

        client.Handler.VerifyNoUnmatchedRequests();
    }

    [Test]
    public async Task VerifyNoUnmatchedRequestsFails()
    {
        using var client = Mock.HttpClient("http://localhost");

        await client.GetAsync("/api/unknown");

        Assert.Throws<MockVerificationException>(() =>
            client.Handler.VerifyNoUnmatchedRequests());
    }

    [Test]
    public async Task ResetClearsSetupsAndRequests()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnGet("/api/data").RespondWithString("data");

        await client.GetAsync("/api/data");

        client.Handler.Reset();

        await Assert.That(client.Handler.Requests).Count().IsEqualTo(0);

        // After reset, no setups so should get default
        var response = await client.GetAsync("/api/data");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateClientWithBaseAddress()
    {
        using var client = Mock.HttpClient("http://example.com");

        await Assert.That(client.BaseAddress).IsNotNull();
        await Assert.That(client.BaseAddress!.Host).IsEqualTo("example.com");
    }

    [Test]
    public async Task OnPutMatchesPutRequests()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnPut("/api/item/1").Respond(HttpStatusCode.NoContent);

        var response = await client.PutAsync("/api/item/1", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task FactoryResponseUsesRequest()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond().WithFactory(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"Echo: {req.Method} {req.RequestUri}")
            });

        var response = await client.GetAsync("/api/echo");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(body).Contains("GET");
        await Assert.That(body).Contains("/api/echo");
    }

    [Test]
    public async Task HasHeaderMatchesPresence()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.HasHeader("X-Api-Key")).Respond(HttpStatusCode.OK);

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
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnRequest(r => r.Matching(req =>
            req.RequestUri?.Query.Contains("page=1") == true))
            .RespondWithJson("""{"page": 1}""");

        var response = await client.GetAsync("/api/data?page=1&size=10");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task MockHttpClient_IsHttpClient()
    {
        using var client = Mock.HttpClient("http://localhost");

        // MockHttpClient IS an HttpClient — can be passed to anything expecting HttpClient
        HttpClient httpClient = client;
        await Assert.That(httpClient).IsNotNull();
    }

    [Test]
    public async Task MockHttpClient_HandlerPropertyAccessible()
    {
        using var client = Mock.HttpClient();
        client.Handler.OnAnyRequest().RespondWithString("hello");

        // Can set base address after creation
        client.BaseAddress = new Uri("http://localhost");
        var response = await client.GetStringAsync("/test");

        await Assert.That(response).IsEqualTo("hello");
    }

    [Test]
    public async Task Verify_WithHeaderMatcher_CountsCorrectly()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/secure");
        request.Headers.Add("Authorization", "Bearer token123");
        await client.SendAsync(request);

        // Should match the request with the header
        client.Handler.Verify(r => r.Header("Authorization", "Bearer token123"), Times.Once);
    }

    [Test]
    public async Task Verify_WithHasHeader_CountsCorrectly()
    {
        using var client = Mock.HttpClient("http://localhost");
        client.Handler.OnAnyRequest().Respond();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/data");
        request.Headers.Add("X-Api-Key", "secret");
        await client.SendAsync(request);

        await client.GetAsync("/api/data");

        // Only one request had the header
        client.Handler.Verify(r => r.HasHeader("X-Api-Key"), Times.Once);
    }
}
