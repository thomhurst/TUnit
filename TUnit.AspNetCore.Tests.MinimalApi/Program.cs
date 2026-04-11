// Program is namespaced (not global, no top-level statements) so this entry point can
// coexist with TUnit.AspNetCore.Tests.WebApp's global Program in the same test assembly
// without ambiguous-reference compile errors.

namespace TUnit.AspNetCore.Tests.MinimalApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        var logger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MinimalApiEndpoints");

        app.MapGet("/log/{marker}", (string marker) =>
        {
            logger.LogInformation("MINIMAL_API_LOG:{Marker}", marker);
            return Results.Ok(new { Marker = marker });
        });

        app.Run();
    }
}
