using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3992;

/// <summary>
/// Once this is discovered during test discovery, containers spin up
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public sealed class RuntimeInitializeTests
{
    //Docker container
    [ClassDataSource<DummyContainer>(Shared = SharedType.PerClass)]
    public required DummyContainer Container { get; init; }

    [Before(Class)]
    public static Task BeforeClass(ClassHookContext context) => NotInitialised(context.Tests);

    [After(TestDiscovery)]
    public static Task AfterDiscovery(TestDiscoveryContext context) => NotInitialised(context.AllTests);

    public static async Task NotInitialised(IEnumerable<TestContext> tests)
    {
        var bugRecreations = tests.Select(x => x.Metadata.TestDetails.ClassInstance).OfType<RuntimeInitializeTests>();

        foreach (var bugRecreation in bugRecreations)
        {
            await Assert.That(bugRecreation.Container).IsNotNull();
            await Assert.That(DummyContainer.NumberOfInits).IsEqualTo(0);
        }
    }

    [Test, Arguments(1)]
    public async Task Test(int value, CancellationToken token)
    {
        await Assert.That(value).IsNotDefault();
        await Assert.That(DummyContainer.NumberOfInits).IsEqualTo(1);
    }

    public class DummyContainer : IAsyncInitializer, IAsyncDisposable
    {
        public Task InitializeAsync()
        {
            NumberOfInits++;
            return Task.CompletedTask;
        }

        public static int NumberOfInits { get; private set; }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }

}
