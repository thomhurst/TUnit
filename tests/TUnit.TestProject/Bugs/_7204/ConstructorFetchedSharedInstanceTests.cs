using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7204;

// A shared instance that a test fetches itself through SharedDataSources is held by that test, but
// is not injected, so the engine must not initialize it. Its initializer fetching another shared
// instance must not break the test's setup either (#7204).

public sealed class ConstructorFetchedInitializer : IAsyncInitializer, IDisposable
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        // Synchronous, before any await: the fetch must not change a collection the engine is reading.
        SharedDataSources.GetOrCreate<SharedAfterDisposalFixture>(SharedType.Keyed, key: "7204-constructor-inner");
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ConstructorFetchedSharedInstanceTests
{
    private readonly ConstructorFetchedInitializer _initializer =
        SharedDataSources.GetOrCreate<ConstructorFetchedInitializer>(SharedType.Keyed, key: "7204-constructor-outer");

    [Test]
    public async Task Instance_Fetched_In_Constructor_Is_Not_Initialized_By_The_Engine()
    {
        await Assert.That(_initializer.IsInitialized).IsFalse();
    }
}
