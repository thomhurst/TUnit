# HTTP Mocking

`TUnit.Mocks.Http` provides `MockHttpHandler` — a drop-in `HttpMessageHandler` replacement for testing code that uses `HttpClient`.

```
dotnet add package TUnit.Mocks.Http
```

## Getting Started[​](#getting-started "Direct link to Getting Started")

```
using TUnit.Mocks;



[Test]

public async Task Fetches_Users_From_Api()

{

    // Arrange — MockHttpClient is a real HttpClient with a .Handler property

    using var client = Mock.HttpClient("https://example.com");

    client.Handler.OnGet("/api/users").RespondWithJson("""[{"id": 1, "name": "Alice"}]""");



    // Act

    var response = await client.GetAsync("/api/users");

    var body = await response.Content.ReadAsStringAsync();



    // Assert

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    await Assert.That(body).Contains("Alice");

}
```

## Creating a Client[​](#creating-a-client "Direct link to Creating a Client")

`Mock.HttpClient()` returns a `MockHttpClient` — a subclass of `HttpClient` with a `.Handler` property for configuring setups and verifying calls:

```
// With base address (most common)

using var clientWithBaseAddress = Mock.HttpClient("https://api.example.com");

_ = clientWithBaseAddress;



// Without base address

using var clientWithoutBaseAddress = Mock.HttpClient();

clientWithoutBaseAddress.BaseAddress = new Uri("https://api.example.com");



// Just the handler (when you need more control)

var handler = Mock.HttpHandler();

using var handlerClient = handler.CreateClient("https://api.example.com");

_ = handlerClient;
```

`MockHttpClient` **is** an `HttpClient` — pass it anywhere `HttpClient` is expected. Use `.Handler` for all setup and verification:

## Setting Up Responses[​](#setting-up-responses "Direct link to Setting Up Responses")

All setup is done through `client.Handler` (or directly on a `MockHttpHandler` if you created one with `Mock.HttpHandler()`).

### By HTTP Method[​](#by-http-method "Direct link to By HTTP Method")

```
client.Handler.OnGet("/api/users").RespondWithJson("""[{"id": 1}]""");

client.Handler.OnPost("/api/users").Respond(HttpStatusCode.Created);

client.Handler.OnPut("/api/users/1").Respond(HttpStatusCode.NoContent);

client.Handler.OnDelete("/api/users/1").Respond(HttpStatusCode.NoContent);
```

### Any Request[​](#any-request "Direct link to Any Request")

```
client.Handler.OnAnyRequest().Respond(HttpStatusCode.OK);
```

### Custom Matching[​](#custom-matching "Direct link to Custom Matching")

Use `OnRequest` with a fluent matcher for complex conditions:

```
// Match by path prefix

client.Handler.OnRequest(r => r.Method(HttpMethod.Get).PathStartsWith("/api/v2"))

    .RespondWithJson("""{"version": 2}""");



// Match by regex

client.Handler.OnRequest(r => r.PathMatches(@"/api/users/\d+"))

    .RespondWithJson("""{"id": 1, "name": "Alice"}""");



// Match by header

client.Handler.OnRequest(r => r.HasHeader("Authorization"))

    .RespondWithJson("""{"authenticated": true}""");



client.Handler.OnRequest(r => r.Header("Authorization", "Bearer valid-token"))

    .RespondWithJson("""{"user": "admin"}""");



// Match by body content

client.Handler.OnRequest(r => r.BodyContains("searchQuery"))

    .RespondWithJson("""{"results": []}""");



// Custom predicate

client.Handler.OnRequest(r => r.Matching(msg => msg.RequestUri?.Port == 8080))

    .Respond(HttpStatusCode.OK);
```

### Request Matcher Reference[​](#request-matcher-reference "Direct link to Request Matcher Reference")

| Method                    | Matches                                 |
| ------------------------- | --------------------------------------- |
| `.Method(HttpMethod)`     | Specific HTTP method                    |
| `.Path(string)`           | Exact path                              |
| `.PathStartsWith(string)` | Path prefix                             |
| `.PathMatches(string)`    | Regex pattern on path                   |
| `.Header(name, value)`    | Header with exact value                 |
| `.HasHeader(name)`        | Header present (any value)              |
| `.BodyContains(string)`   | Request body contains text              |
| `.Matching(predicate)`    | Custom `Func<HttpRequestMessage, bool>` |

## Response Configuration[​](#response-configuration "Direct link to Response Configuration")

### Basic Responses[​](#basic-responses "Direct link to Basic Responses")

```
// Status code only

client.Handler.OnGet("/health").Respond(HttpStatusCode.OK);



// JSON body

client.Handler.OnGet("/api/data").RespondWithJson("""{"key": "value"}""");



// Plain text body

client.Handler.OnGet("/api/version").RespondWithString("1.0.0");
```

### Response Builder[​](#response-builder "Direct link to Response Builder")

For more control, use the response builder:

```
client.Handler.OnGet("/api/data")

    .Respond(HttpStatusCode.OK)

    .WithJsonContent("""{"key": "value"}""")

    .WithHeader("X-Request-Id", "abc123");
```

### Dynamic Responses[​](#dynamic-responses "Direct link to Dynamic Responses")

Build responses based on the incoming request:

```
client.Handler.OnPost("/api/echo")

    .Respond()

    .WithFactory(request =>

    {

        var body = request.Content?.ReadAsStringAsync().Result ?? "";

        return new HttpResponseMessage(HttpStatusCode.OK)

        {

            Content = new StringContent(body)

        };

    });
```

### Simulating Delays[​](#simulating-delays "Direct link to Simulating Delays")

```
client.Handler.OnGet("/api/slow")

    .Respond(HttpStatusCode.OK)

    .WithDelay(TimeSpan.FromSeconds(2));
```

### Throwing Exceptions[​](#throwing-exceptions "Direct link to Throwing Exceptions")

```
client.Handler.OnGet("/api/failing")

    .Throws("Connection refused");



client.Handler.OnGet("/api/timeout")

    .Throws(new TaskCanceledException("Request timed out"));
```

## Sequential Responses[​](#sequential-responses "Direct link to Sequential Responses")

Return different responses for successive requests to the same endpoint:

```
var setup = client.Handler.OnGet("/api/status");

setup.RespondWithString("starting");

setup.Then().RespondWithString("running");

setup.Then().RespondWithString("complete");



// 1st call: "starting"

// 2nd call: "running"

// 3rd+ calls: "complete" (last response repeats)
```

## Unmatched Requests[​](#unmatched-requests "Direct link to Unmatched Requests")

By default, unmatched requests return **404 Not Found**. You can change this:

```
// Change default status code

client.Handler.WithDefaultStatus(HttpStatusCode.ServiceUnavailable);



// Or throw on unmatched requests

client.Handler.ThrowOnUnmatched();
```

## Verification[​](#verification "Direct link to Verification")

### Verify Call Count[​](#verify-call-count "Direct link to Verify Call Count")

```
client.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Once);

client.Handler.Verify(r => r.Method(HttpMethod.Delete), Times.Never);
```

### Verify No Unmatched Requests[​](#verify-no-unmatched-requests "Direct link to Verify No Unmatched Requests")

```
client.Handler.VerifyNoUnmatchedRequests();
```

### Inspect Captured Requests[​](#inspect-captured-requests "Direct link to Inspect Captured Requests")

```
await Assert.That(client.Handler.Requests).Count().IsEqualTo(2);

await Assert.That(client.Handler.Requests[0].Method).IsEqualTo(HttpMethod.Get);

await Assert.That(client.Handler.Requests[0].RequestUri!.PathAndQuery).IsEqualTo("/api/users");



// Check for unmatched requests

await Assert.That(client.Handler.UnmatchedRequests).Count().IsEqualTo(0);
```

Each `CapturedRequest` provides:

| Property     | Description                          |
| ------------ | ------------------------------------ |
| `Method`     | HTTP method                          |
| `RequestUri` | Full request URI                     |
| `Body`       | Request body as string (or null)     |
| `Headers`    | Request headers                      |
| `Matched`    | Whether a setup matched this request |
| `Timestamp`  | When the request was captured        |

## Mocking `IHttpClientFactory`[​](#mocking-ihttpclientfactory "Direct link to mocking-ihttpclientfactory")

`Mock.HttpClientFactory()` returns a factory whose `CreateClient` produces non-disposing `HttpClient`s sharing one `MockHttpHandler`, so captured requests survive `using` blocks in the system under test.

```
var factory = Mock.HttpClientFactory().WithBaseAddress("https://api.example.com");

factory.Handler.OnGet("/api/users").RespondWithJson("""[{"id":1}]""");



var sut = new Sut(factory);

await sut.DoWork(); // SUT may call CreateClient() any number of times



factory.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Once);
```

### Named clients[​](#named-clients "Direct link to Named clients")

For typed/named clients registered via `services.AddHttpClient("users")`, assign a dedicated handler (and optionally base address) per name. Name lookups are case-insensitive, matching `IHttpClientFactory` semantics. Unregistered names fall back to `factory.Handler`.

```
var factory = Mock.HttpClientFactory()

    .WithHandler("users", Mock.HttpHandler())

    .WithHandler("orders", Mock.HttpHandler())

    .WithBaseAddress("users", "https://users.example.com")

    .WithBaseAddress("orders", "https://orders.example.com");



factory.HandlerFor("users").OnGet("/").RespondWithJson("""[]""");

factory.HandlerFor("orders").OnPost("/").Respond(HttpStatusCode.Created);
```

## Reset[​](#reset "Direct link to Reset")

```
client.Handler.Reset(); // clears all setups and captured requests
```
