var builder = WebApplication.CreateBuilder(args);

// Register services that can be replaced in tests
builder.Services.AddSingleton<IGreetingService, DefaultGreetingService>();
builder.Services.AddSingleton<ITimeService, SystemTimeService>();

var app = builder.Build();

// Basic ping endpoint
app.MapGet("/ping", () => "pong");

// Greeting endpoint using injected service
app.MapGet("/greet/{name}", (string name, IGreetingService greetingService) =>
    greetingService.GetGreeting(name));

// Time endpoint using injected service
app.MapGet("/time", (ITimeService timeService) =>
    new { CurrentTime = timeService.GetCurrentTime() });

// Configuration endpoint for testing config overrides
app.MapGet("/config/message", (IConfiguration config) =>
    config["TestMessage"] ?? "default message");

// Echo endpoint for testing request/response capture
app.MapPost("/echo", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Ok(new { Echo = body, Received = DateTime.UtcNow });
});

// Status endpoint that returns headers for testing
app.MapGet("/status", (HttpContext context) =>
{
    context.Response.Headers.Append("X-Custom-Header", "test-value");
    return Results.Ok(new { Status = "healthy" });
});

app.Run();

// Service interfaces and implementations
public interface IGreetingService
{
    string GetGreeting(string name);
}

public class DefaultGreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}!";
}

public interface ITimeService
{
    DateTime GetCurrentTime();
}

public class SystemTimeService : ITimeService
{
    public DateTime GetCurrentTime() => DateTime.UtcNow;
}

// Required for WebApplicationFactory
public partial class Program;
