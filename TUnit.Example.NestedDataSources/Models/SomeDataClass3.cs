using TUnit.Core.Interfaces;

namespace TUnit.Example.NestedDataSources.Models;

public record SomeDataClass3 : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}