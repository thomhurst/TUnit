using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry so logs carry the inherited TraceId and are exported via OTLP.
// OTEL_EXPORTER_OTLP_ENDPOINT and OTEL_EXPORTER_OTLP_PROTOCOL are injected by AspireFixture.
// OTEL_SERVICE_NAME is injected automatically by TUnit's AspireFixture from the
// Aspire resource name, so .ConfigureResource() is not needed.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithLogging(logging => logging.AddOtlpExporter());

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

// Simple health endpoint.
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
