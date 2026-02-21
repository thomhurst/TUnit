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
using TUnit.Mocks.Http;

[Test]
public async Task Fetches_Users_From_Api()
{
    // Arrange
    var handler = new MockHttpHandler();
    handler.OnGet("/api/users").RespondWithJson("""[{"id": 1, "name": "Alice"}]""");

    using var client = handler.CreateClient("https://example.com");

    // Act
    var response = await client.GetAsync("/api/users");
    var body = await response.Content.ReadAsStringAsync();

    // Assert
    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    await Assert.That(body).Contains("Alice");
}
```

## Creating a Client

```csharp
var handler = new MockHttpHandler();

// With base address
using var client = handler.CreateClient("https://api.example.com");

// Without base address
using var client = handler.CreateClient();
```

## Setting Up Responses

### By HTTP Method

```csharp
handler.OnGet("/api/users").RespondWithJson("""[{"id": 1}]""");
handler.OnPost("/api/users").Respond(HttpStatusCode.Created);
handler.OnPut("/api/users/1").Respond(HttpStatusCode.NoContent);
handler.OnDelete("/api/users/1").Respond(HttpStatusCode.NoContent);
```

### Any Request

```csharp
handler.OnAnyRequest().Respond(HttpStatusCode.OK);
```

### Custom Matching

Use `OnRequest` with a fluent matcher for complex conditions:

```csharp
// Match by path prefix
handler.OnRequest(r => r.Method(HttpMethod.Get).PathStartsWith("/api/v2"))
    .RespondWithJson("""{"version": 2}""");

// Match by regex
handler.OnRequest(r => r.PathMatches(@"/api/users/\d+"))
    .RespondWithJson("""{"id": 1, "name": "Alice"}""");

// Match by header
handler.OnRequest(r => r.HasHeader("Authorization"))
    .RespondWithJson("""{"authenticated": true}""");

handler.OnRequest(r => r.Header("Authorization", "Bearer valid-token"))
    .RespondWithJson("""{"user": "admin"}""");

// Match by body content
handler.OnRequest(r => r.BodyContains("searchQuery"))
    .RespondWithJson("""{"results": []}""");

// Custom predicate
handler.OnRequest(r => r.Matching(msg => msg.RequestUri?.Port == 8080))
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
handler.OnGet("/health").Respond(HttpStatusCode.OK);

// JSON body
handler.OnGet("/api/data").RespondWithJson("""{"key": "value"}""");

// Plain text body
handler.OnGet("/api/version").RespondWithString("1.0.0");
```

### Response Builder

For more control, use the response builder:

```csharp
handler.OnGet("/api/data")
    .Respond(HttpStatusCode.OK)
    .WithJsonContent("""{"key": "value"}""")
    .WithHeader("X-Request-Id", "abc123");
```

### Dynamic Responses

Build responses based on the incoming request:

```csharp
handler.OnPost("/api/echo")
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
handler.OnGet("/api/slow")
    .Respond(HttpStatusCode.OK)
    .WithDelay(TimeSpan.FromSeconds(2));
```

### Throwing Exceptions

```csharp
handler.OnGet("/api/failing")
    .Throws("Connection refused");

handler.OnGet("/api/timeout")
    .Throws(new TaskCanceledException("Request timed out"));
```

## Sequential Responses

Return different responses for successive requests to the same endpoint:

```csharp
var setup = handler.OnGet("/api/status");
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
handler.WithDefaultStatus(HttpStatusCode.ServiceUnavailable);

// Or throw on unmatched requests
handler.ThrowOnUnmatched();
```

## Verification

### Verify Call Count

```csharp
handler.Verify(r => r.Method(HttpMethod.Get).Path("/api/users"), Times.Once);
handler.Verify(r => r.Method(HttpMethod.Delete), Times.Never);
```

### Verify No Unmatched Requests

```csharp
handler.VerifyNoUnmatchedRequests();
```

### Inspect Captured Requests

```csharp
await Assert.That(handler.Requests).HasCount().EqualTo(2);
await Assert.That(handler.Requests[0].Method).IsEqualTo(HttpMethod.Get);
await Assert.That(handler.Requests[0].RequestUri!.PathAndQuery).IsEqualTo("/api/users");

// Check for unmatched requests
await Assert.That(handler.UnmatchedRequests).HasCount().EqualTo(0);
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
handler.Reset(); // clears all setups and captured requests
```
