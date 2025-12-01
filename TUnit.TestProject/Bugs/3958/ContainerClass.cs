using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Simulates a container like PulsarTestContainer that implements IAsyncInitializer.
/// This is the innermost nested data source in the hierarchy.
/// </summary>
public class ContainerClass : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public string ConnectionString { get; private set; } = "";

    public Task InitializeAsync()
    {
        Console.WriteLine("ContainerClass.InitializeAsync called");
        IsInitialized = true;
        ConnectionString = "container://localhost:12345";
        return Task.CompletedTask;
    }
}
