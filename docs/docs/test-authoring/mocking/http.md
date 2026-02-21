---
sidebar_position: 6
---

# HTTP Mocking

`TUnit.Mocks.Http` provides `MockHttpHandler` — a drop-in `HttpMessageHandler` replacement for testing code that uses `HttpClient`.

```bash
dotnet add package TUnit.Mocks.Http --prerelease
```

## Getting Started

```csharp
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

## Creating a Client

`Mock.HttpClient()` returns a `MockHttpClient` — a subclass of `HttpClient` with a `.Handler` property for configuring setups and verifying calls:

```csharp
// With base address (most common)
using var client = Mock.HttpClient("https://api.example.com");

// Without base address
using var client = Mock.HttpClient();
client.BaseAddress = new Uri("https://api.example.com");

// Just the handler (when you need more control)
var handler = Mock.HttpHandler();
using var client = handler.CreateClient("https://api.example.com");
```

`MockHttpClient` **is** an `HttpClient` — pass it anywhere `HttpClient` is expected. Use `.Handler` for all setup and verification:

## Setting Up Responses

All setup is done through `client.Handler` (or directly on a `MockHttpHandler` if you created one with `Mock.HttpHandler()`).

### By HTTP Method

```csharp
client.Handler.OnGet("/api/users").RespondWithJson("""[{"id": 1}]""");
client.Handler.OnPost("/api/users").Respond(HttpStatusCode.Created);
client.Handler.OnPut("/api/users/1").Respond(HttpStatusCode.NoContent);
client.Handler.OnDelete("/api/users/1").Respond(HttpStatusCode.NoContent);
```

### Any Request

```csharp
client.Handler.OnAnyRequest().Respond(HttpStatusCode.OK);
```

### Custom Matching

Use `OnRequest` with a fluent matcher for complex conditions:

```csharp
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

### Request Matcher Reference

| Method | Matches |
|---|---|
| `.Method(HttpMethod)` | Specific HTTP method |
| `.Path(string)` | Exact path |
| `.PathStartsWith(string)` | Path prefix |
| `.PathMatches(string)` | Regex pattern on path |
| `.Header(name, value)` | Header with exact value |
| `.HasHeader(name)` | Header present (any value) |
| `.BodyContains(string)` | Request body contains text |
| `.Matching(predicate)` | Custom `Func<HttpRequestMessage, bool>` |

## Response Configuration

### Basic Responses

```csharp
// Status code only
client.Handler.OnGet("/health").Respond(HttpStatusCode.OK);

// JSON body
client.Handler.OnGet("/api/data").RespondWithJson("""{"key": "value"}""");

// Plain text body
client.Handler.OnGet("/api/version").RespondWithString("1.0.0");
```

### Response Builder

For more control, use the response builder:

```csharp
client.Handler.OnGet("/api/data")
    .Respond(HttpStatusCode.OK)
    .WithJsonContent("""{"key": "value"}""")
    .WithHeader("X-Request-Id", "abc123");
```

### Dynamic Responses

Build responses based on the incoming request:

```csharp
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

### Simulating Delays

```csharp
client.Handler.OnGet("/api/slow")
    .Respond(HttpStatusCode.OK)
    .WithDelay(TimeSpan.FromSeconds(2));
```

### Throwing Exceptions

```csharp
client.Handler.OnGet("/api/failing")
    .Throws("Connection refused");

client.Handler.OnGet("/api/timeout")
    .Throws(new TaskCanceledException("Request timed out"));
```

## Sequential Responses

Return different responses for successive requests to the same endpoint:

```csharp
var setup = client.Handler.OnGet("/api/status");
setup.RespondWithString("starting");
setup.Then().RespondWithString("running");
setup.Then().RespondWithString("complete");

// 1st call: "starting"
// 2nd call: "running"
// 3rd+ calls: "complete" (last response repeats)
```

## Unmatched Requests

By default, unmatched requests return **404 Not Found**. You can change this:

```csharp
// Change default status code
client.Handler.WithDefaultStatus(HttpStatusCode.ServiceUnavailable);

// Or throw on unmatched requests
client.Handler.ThrowOnUnmatched();
```

## Verification

### Verify Call Count

```csharp
client.Handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Once);
client.Handler.Verify(r => r.Method(HttpMethod.Delete), Times.Never);
```

### Verify No Unmatched Requests

```csharp
client.Handler.VerifyNoUnmatchedRequests();
```

### Inspect Captured Requests

```csharp
await Assert.That(client.Handler.Requests).HasCount().EqualTo(2);
await Assert.That(client.Handler.Requests[0].Method).IsEqualTo(HttpMethod.Get);
await Assert.That(client.Handler.Requests[0].RequestUri!.PathAndQuery).IsEqualTo("/api/users");

// Check for unmatched requests
await Assert.That(client.Handler.UnmatchedRequests).HasCount().EqualTo(0);
```

Each `CapturedRequest` provides:

| Property | Description |
|---|---|
| `Method` | HTTP method |
| `RequestUri` | Full request URI |
| `Body` | Request body as string (or null) |
| `Headers` | Request headers |
| `Matched` | Whether a setup matched this request |
| `Timestamp` | When the request was captured |

## Reset

```csharp
client.Handler.Reset(); // clears all setups and captured requests
```
