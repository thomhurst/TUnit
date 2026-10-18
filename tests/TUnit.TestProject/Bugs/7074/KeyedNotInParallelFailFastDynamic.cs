using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7074;

public sealed class FailFastDynamicSessionFixture7074 : IAsyncDisposable
{
    public static int Disposed;

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref Disposed, 1);
        return default;
    }
}

/// <summary>
/// With --fail-fast and --maximum-parallel-tests 2, Fails fails while a dynamically created
/// variant is still running and ignores cancellation. The queued keyed tests then throw on
/// the cancelled slot wait, so the main phases exit early. Session teardown (static property
/// and shared fixture disposal) must still wait for the in-flight dynamic test.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class KeyedNotInParallelFailFastDynamic7074
{
    private static readonly TaskCompletionSource<bool> VariantStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [ClassDataSource<FailFastDynamicSessionFixture7074>(Shared = SharedType.PerTestSession)]
    public required FailFastDynamicSessionFixture7074 Fixture { get; init; }

    // Critical priority so Spawner takes a slot ahead of everything else. Its variant is
    // queued as a dynamic test and takes the slot Spawner releases.
    [Test, ExecutionPriority(Priority.Critical), Arguments(false)]
    public async Task Spawner(bool isVariant)
    {
        if (!isVariant)
        {
            await TestContext.Current!.CreateTestVariant(methodArguments: [true], displayName: "Spawner variant");
            return;
        }

        VariantStarted.TrySetResult(true);

        // Deliberately ignores cancellation, so it is still running after fail-fast.
        await Task.Delay(2000);

        // No-op unless the engine test opted in via the environment variable.
        var markerPath = Environment.GetEnvironmentVariable("TUNIT_7074_DYNAMIC_MARKER_PATH");
        if (!string.IsNullOrEmpty(markerPath))
        {
            File.WriteAllText(markerPath, $"FixtureDisposedDuringDynamicTest={Volatile.Read(ref FailFastDynamicSessionFixture7074.Disposed) == 1}");
        }
    }

    // Holds the shared key, so the Queued tests wait on the key (not a slot) until it fails.
    [Test, NotInParallel("7074.dyn"), ExecutionPriority(Priority.Critical)]
    public async Task Fails()
    {
        await VariantStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        throw new InvalidOperationException("Intentional failure to trigger fail-fast");
    }

    [Test, NotInParallel("7074.dyn")] public Task Queued1() => Task.Delay(100);
    [Test, NotInParallel("7074.dyn")] public Task Queued2() => Task.Delay(100);
}
