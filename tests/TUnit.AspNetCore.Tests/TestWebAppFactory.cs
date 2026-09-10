using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TUnit.AspNetCore.Tests;

/// <summary>
/// Shared web application factory for integration tests.
/// </summary>
public class TestWebAppFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // Eagerly start the server to catch configuration errors early
        _ = Server;
        return Task.CompletedTask;
    }
}
