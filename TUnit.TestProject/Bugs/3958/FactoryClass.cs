using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Simulates a WebApplicationFactory-like class that implements IAsyncInitializer.
/// Used at the base IntegrationTestBase level.
/// </summary>
public class FactoryClass : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public string BaseUrl { get; private set; } = "";

    public Task InitializeAsync()
    {
        Console.WriteLine("FactoryClass.InitializeAsync called");
        IsInitialized = true;
        BaseUrl = "http://localhost:5000";
        return Task.CompletedTask;
    }
}
