var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("downstream")
    .ConfigurePrimaryHttpMessageHandler(() => new HeaderEchoHandler());

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

// Outbound call through IHttpClientFactory. The downstream pipeline's primary
// handler echoes request headers back in the response body so tests can assert
// which headers the SUT-side HttpClient actually emitted.
app.MapGet("/proxy", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("downstream");
    var response = await client.GetAsync("http://downstream.test/");
    var body = await response.Content.ReadAsStringAsync();
    return Results.Content(body, "text/plain");
});

app.Run();

public partial class Program;

internal sealed class HeaderEchoHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var dump = string.Join("\n", request.Headers.SelectMany(h => h.Value.Select(v => $"{h.Key}: {v}")));
        return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(dump)
        });
    }
}
