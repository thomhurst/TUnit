using TUnit.Core.Extensions;
using TUnit.TestProject.Attributes;

#pragma warning disable WIP

namespace TUnit.TestProject.Bugs._7204;

// When the last test holding a shared instance ends, the instance is disposed. A test that asks
// for the same shared instance afterwards must get a new one, not the disposed one: both a
// dynamic test registered later and a running test that fetches it through SharedDataSources (#7204).

public sealed class SharedAfterDisposalFixture : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

// Not a test class: only used as the class of the dynamic test, so its property is the only holder.
public sealed class SharedAfterDisposalDynamicConsumer
{
    public static SharedAfterDisposalFixture? Disposed { get; set; }

    [ClassDataSource<SharedAfterDisposalFixture>(Shared = SharedType.Keyed, Key = SharedInstanceAfterDisposalTests.DynamicKey)]
    public required SharedAfterDisposalFixture Fixture { get; init; }

    // Synchronous: the dynamic test's Action<T> expression would discard a returned Task, and with it any failure.
    public void Receives_A_Live_Instance()
    {
        if (Fixture.IsDisposed || ReferenceEquals(Fixture, Disposed))
        {
            throw new InvalidOperationException("The dynamic test received the disposed shared instance.");
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
public class SharedInstanceAfterDisposalTests
{
    public const string DynamicKey = "7204-dynamic";
    private const string FetchedKey = "7204-fetched";

    private static SharedAfterDisposalFixture? _dynamicHolderFixture;
    private static SharedAfterDisposalFixture? _fetchedHolderFixture;

    [Test]
    [ClassDataSource<SharedAfterDisposalFixture>(Shared = SharedType.Keyed, Key = DynamicKey)]
    public void Dynamic_Holder(SharedAfterDisposalFixture fixture) => _dynamicHolderFixture = fixture;

    [Test]
    [ClassDataSource<SharedAfterDisposalFixture>(Shared = SharedType.Keyed, Key = FetchedKey)]
    public void Fetched_Holder(SharedAfterDisposalFixture fixture) => _fetchedHolderFixture = fixture;

    [Test, DependsOn(nameof(Dynamic_Holder), new[] { typeof(SharedAfterDisposalFixture) })]
    public async Task Dynamic_Test_Registered_After_Disposal_Gets_A_New_Instance()
    {
        await WaitUntilDisposed(_dynamicHolderFixture!);
        SharedAfterDisposalDynamicConsumer.Disposed = _dynamicHolderFixture;

        await TestContext.Current!.AddDynamicTest(new DynamicTest<SharedAfterDisposalDynamicConsumer>
        {
            TestMethod = consumer => consumer.Receives_A_Live_Instance(),
            Attributes = []
        });
    }

    [Test, DependsOn(nameof(Fetched_Holder), new[] { typeof(SharedAfterDisposalFixture) })]
    public async Task Running_Test_Fetching_After_Disposal_Gets_A_New_Instance()
    {
        await WaitUntilDisposed(_fetchedHolderFixture!);

        var fixture = SharedDataSources.GetOrCreate<SharedAfterDisposalFixture>(SharedType.Keyed, key: FetchedKey);

        await Assert.That(fixture.IsDisposed).IsFalse();
        await Assert.That(fixture).IsNotSameReferenceAs(_fetchedHolderFixture);
    }

    // The holder's references are released just after it is reported complete, so wait for that.
    private static async Task WaitUntilDisposed(SharedAfterDisposalFixture fixture)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!fixture.IsDisposed)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The holder's fixture was not disposed.");
            }

            await Task.Delay(10);
        }
    }
}
