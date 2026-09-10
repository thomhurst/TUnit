using System.Net;
using TUnit.AspNetCore;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.AspNetCore.Tests.MinimalApi;

/// <summary>
/// Regression coverage for thomhurst/TUnit#5503: <see cref="TestWebApplicationFactory{TEntryPoint}"/>
/// must auto-register correlated logging for minimal API hosts. <see cref="MinimalApiTestFactory"/>
/// has zero overrides — if these tests pass, the base class wired up logging on its own.
/// </summary>
public class MinimalApiAutoRegistrationTests
{
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public MinimalApiTestFactory Factory { get; set; } = null!;

    [Test]
    public async Task ServerLog_AutoCorrelated_OnMinimalApiHost_WithoutSubclassConfig()
    {
        var marker = Guid.NewGuid().ToString("N");
        using var client = Factory.CreateClient();

        var response = await client.GetAsync($"/log/{marker}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains($"MINIMAL_API_LOG:{marker}");
    }
}

public class MinimalApiTestFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        // Eagerly start the server to surface configuration errors early
        _ = Server;
        return Task.CompletedTask;
    }
}
