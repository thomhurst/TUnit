using TUnit.Core.Interfaces;

namespace TUnit.Example.NestedDataSources.Models;

public record SomeDataClass1(SomeDataClass2 SomeDataClass2) : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}
