using System.Diagnostics;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
const string ActivitySourceName = "TUnit.Aspire.Tests.ApiService";
var activitySource = new ActivitySource(ActivitySourceName);

// Configure OpenTelemetry so logs carry the inherited TraceId and are exported via OTLP.
// OTEL_EXPORTER_OTLP_ENDPOINT and OTEL_EXPORTER_OTLP_PROTOCOL are injected by AspireFixture.
// OTEL_SERVICE_NAME is injected automatically by TUnit's AspireFixture from the
// Aspire resource name, so .ConfigureResource() is not needed.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(ActivitySourceName)
        .AddOtlpExporter())
    .WithLogging(logging => logging.AddOtlpExporter());
builder.Services.AddHttpClient();

// Ensure formatted messages and scopes are included in OTLP log exports.
builder.Logging.AddOpenTelemetry(otel =>
{
    otel.IncludeFormattedMessage = true;
    otel.IncludeScopes = true;
});

var app = builder.Build();

// Endpoint that logs a caller-specified message, allowing tests to inject unique markers.
app.MapGet("/log", (string message, ILogger<Program> logger) =>
{
    logger.LogInformation("{Message}", message);
    return Results.Ok(new { logged = message });
});

// Endpoint that logs at a specific severity level.
app.MapGet("/log-level", (string message, string level, ILogger<Program> logger) =>
{
    var logLevel = Enum.Parse<LogLevel>(level, ignoreCase: true);
    logger.Log(logLevel, "{Message}", message);
    return Results.Ok(new { logged = message, level });
});

// Endpoint used for backend trace validation. It creates internal spans and two
// nested HTTP calls so trace backends show a real waterfall instead of a single span.
app.MapGet("/trace-demo", async (
    string message,
    HttpRequest request,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    using var requestPreparation = activitySource.StartActivity("trace demo prepare");
    logger.LogInformation("trace-demo start {Message}", message);

    var client = httpClientFactory.CreateClient();
    client.BaseAddress = new Uri($"{request.Scheme}://{request.Host}");

    using var firstCall = activitySource.StartActivity("trace demo health check");
    using var healthResponse = await client.GetAsync("/health");
    healthResponse.EnsureSuccessStatusCode();

    using var secondCall = activitySource.StartActivity("trace demo nested log call");
    using var nestedLogResponse = await client.GetAsync(
        $"/log-level?message={Uri.EscapeDataString($"nested-{message}")}&level=Warning");
    nestedLogResponse.EnsureSuccessStatusCode();

    logger.LogInformation("trace-demo complete {Message}", message);

    return Results.Ok(new
    {
        message,
        nestedMessage = $"nested-{message}"
    });
});

// Simple health endpoint.
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
