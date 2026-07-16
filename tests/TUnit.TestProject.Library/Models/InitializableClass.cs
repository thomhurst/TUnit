using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Library.Models;

public class InitializableClass : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public bool IsInitialized { get; private set; }
}
