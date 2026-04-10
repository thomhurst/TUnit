var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Endpoints");

// Endpoint that logs a caller-provided marker so we can verify log routing.
// The marker appears in the server-side ILogger output; the test verifies
// that it ends up in the correct TestContext's captured output.
app.MapGet("/log/{marker}", (string marker) =>
{
    logger.LogInformation("SERVER_LOG:{Marker}", marker);
    return Results.Ok(new { Marker = marker });
});

app.MapGet("/ping", () => "pong");

app.Run();

public partial class Program;
