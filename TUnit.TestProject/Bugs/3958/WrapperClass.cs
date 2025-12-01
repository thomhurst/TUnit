using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._3958;

/// <summary>
/// Simulates PulsarBrokerHandlerTestWrapper that:
/// 1. Implements IAsyncInitializer and IAsyncDisposable
/// 2. Has its own nested ClassDataSource property (ContainerClass)
/// 3. Verifies the container is initialized before this wrapper's InitializeAsync is called
/// </summary>
public class WrapperClass : IAsyncInitializer, IAsyncDisposable
{
    [ClassDataSource<ContainerClass>(Shared = SharedType.PerTestSession)]
    public required ContainerClass Container { get; init; }

    public bool IsInitialized { get; private set; }
    public bool ContainerWasInitializedFirst { get; private set; }

    public Task InitializeAsync()
    {
        Console.WriteLine("WrapperClass.InitializeAsync called");

        // This is the key assertion: Container should be injected and initialized
        // BEFORE this wrapper's InitializeAsync is called
        if (Container == null)
        {
            throw new InvalidOperationException(
                "Container property is null in WrapperClass.InitializeAsync - " +
                "nested data source was not initialized before calling InitializeAsync");
        }

        ContainerWasInitializedFirst = Container.IsInitialized;

        if (!ContainerWasInitializedFirst)
        {
            throw new InvalidOperationException(
                "Container.IsInitialized is false in WrapperClass.InitializeAsync - " +
                "Container.InitializeAsync was not called before WrapperClass.InitializeAsync");
        }

        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine("WrapperClass.DisposeAsync called");
        return default;
    }
}
