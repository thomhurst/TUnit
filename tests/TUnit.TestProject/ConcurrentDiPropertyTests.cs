using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class ConcurrentDiPropertyTests : IDisposable
{
    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property0 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property1 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property2 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property3 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property4 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property5 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property6 { get; init; }

    [ConcurrentPropertySource]
    public required ConcurrentPropertyService Property7 { get; init; }

    internal static readonly ConcurrentBag<ConcurrentPropertyService[]> Completed = [];

    private ConcurrentPropertyService[] Services =>
        [Property0, Property1, Property2, Property3, Property4, Property5, Property6, Property7];

    [Test, Repeat(3), EngineTest(ExpectedResult.Pass)]
    public async Task EveryPropertyIsInitializedAndAlive()
    {
        foreach (var service in Services)
        {
            await Assert.That(service.Initialized).IsTrue();
            await Assert.That(service.DisposeCount).IsEqualTo(0);
            await Assert.That(service.Scope.DisposeCount).IsEqualTo(0);
        }
    }

    public void Dispose()
    {
        var services = Services;
        if (services.Any(service => service.DisposeCount != 0))
            throw new InvalidOperationException("Property scope disposed before its root");
        Completed.Add(services);
    }
}

public class ConcurrentDiPropertyCleanup
{
    [After(TestSession)]
    public static async Task EveryScopeAndServiceIsDisposedOnce()
    {
        foreach (var row in ConcurrentDiPropertyTests.Completed)
        {
            await Assert.That(row.Length).IsEqualTo(8);
            foreach (var service in row)
            {
                await Assert.That(service.DisposeCount).IsEqualTo(1);
                await Assert.That(service.Scope.DisposeCount).IsEqualTo(1);
            }
        }
    }
}

public sealed class ConcurrentPropertySourceAttribute : AsyncUntypedDataSourceGeneratorAttribute
{
    private static readonly ConditionalWeakTable<TestContextEvents, Gate> Gates = new();

    protected override async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
    {
        var gate = Gates.GetValue(metadata.TestBuilderContext.Current.Events, _ => new Gate());
        await foreach (var factory in new PropertyDiSource().GenerateAsync(metadata))
        {
            yield return async () =>
            {
                // All eight property resolutions must be in flight before DI registration starts.
                await gate.ArriveAsync().WaitAsync(TimeSpan.FromSeconds(30));
                return await factory();
            };
        }
    }

    private sealed class Gate
    {
        private int _arrivals;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task ArriveAsync()
        {
            if (Interlocked.Increment(ref _arrivals) == 8) _ready.SetResult();
            return _ready.Task;
        }
    }

    private sealed class PropertyDiSource : DependencyInjectionDataSourceAttribute<ConcurrentPropertyScope>
    {
        public override ConcurrentPropertyScope CreateScope(DataGeneratorMetadata metadata) => new();
        public override object Create(ConcurrentPropertyScope scope, Type type) => scope.Service;
    }
}

public sealed class ConcurrentPropertyScope : IAsyncDisposable
{
    public ConcurrentPropertyScope() => Service = new ConcurrentPropertyService(this);
    public ConcurrentPropertyService Service { get; }
    public int DisposeCount;
    public async ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref DisposeCount);
        await Service.DisposeAsync();
    }
}

public sealed class ConcurrentPropertyService(ConcurrentPropertyScope scope) : IAsyncInitializer, IAsyncDisposable
{
    public ConcurrentPropertyScope Scope { get; } = scope;
    public bool Initialized;
    public int DisposeCount;
    public async Task InitializeAsync()
    {
        await Task.Yield();
        Initialized = true;
    }
    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        Interlocked.Increment(ref DisposeCount);
    }
}
