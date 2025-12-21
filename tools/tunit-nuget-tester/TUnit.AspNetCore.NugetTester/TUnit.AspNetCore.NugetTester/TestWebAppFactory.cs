using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TUnit.AspNetCore.NugetTester;

/// <summary>
/// Factory for integration tests. Extends TestWebApplicationFactory to support
/// per-test isolation via the WebApplicationTest pattern.
/// </summary>
public class TestWebAppFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private int _configuredWebHostCalled;

    /// <summary>
    /// Tracks how many times ConfiguredWebHost was called, useful for verifying factory behavior.
    /// </summary>
    public int ConfiguredWebHostCalled => _configuredWebHostCalled;

    public Task InitializeAsync()
    {
        // Eagerly start the server to catch configuration errors early
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        Interlocked.Increment(ref _configuredWebHostCalled);
    }
}
