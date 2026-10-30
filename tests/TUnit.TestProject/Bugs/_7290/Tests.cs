using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._7290;

/// <summary>
/// Reproduction for issue #7290: "ITestRegisteredEventReceiver is not invoked on property-injected
/// data source objects."
/// https://github.com/thomhurst/TUnit/issues/7290
///
/// Registered receivers ran once, before argument registration resolved injected property values,
/// so a shared fixture received OnTestRegistered as a constructor or method argument but never as
/// an injected property. The fixture below limits its consumers to one test at a time.
/// </summary>
public sealed class OneAtATime : IParallelLimit
{
    public int Limit => 1;
}

public sealed class LimitedFixture : ITestRegisteredEventReceiver
{
    private readonly ConcurrentDictionary<string, int> _registrations = new();
    private int _running;
    private int _maxRunning;

    public int Order => 0;

    public int MaxRunning => Volatile.Read(ref _maxRunning);

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        _registrations.AddOrUpdate(context.TestContext.Metadata.TestDetails.TestId, 1, static (_, count) => count + 1);
        context.SetParallelLimiter(new OneAtATime());
        return default;
    }

    public int RegistrationsFor(TestContext context) =>
        _registrations.TryGetValue(context.Metadata.TestDetails.TestId, out var count) ? count : 0;

    public async Task RunAsync()
    {
        var running = Interlocked.Increment(ref _running);
        int max;
        while (running > (max = Volatile.Read(ref _maxRunning)) &&
               Interlocked.CompareExchange(ref _maxRunning, running, max) != max)
        {
        }

        await Task.Delay(50);
        Interlocked.Decrement(ref _running);
    }
}

public sealed class SkippingFixture : ITestRegisteredEventReceiver
{
    public int Order => 0;

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetSkipped("Skipped by property-injected fixture");
        return default;
    }
}

public sealed class PropertyConsumerTests
{
    [ClassDataSource<LimitedFixture>(Shared = SharedType.PerClass)]
    public required LimitedFixture Fixture { get; init; }

    [Test, Repeat(3)]
    public async Task Property_Injected_Fixture_Receives_OnTestRegistered()
    {
        var context = TestContext.Current!;

        await Fixture.RunAsync();

        await Assert.That(context.Parallelism.Limiter).IsTypeOf<OneAtATime>();
        await Assert.That(Fixture.RegistrationsFor(context)).IsEqualTo(1);
        await Assert.That(Fixture.MaxRunning).IsEqualTo(1);
    }
}

[ClassDataSource<LimitedFixture>(Shared = SharedType.PerClass)]
public sealed class ConstructorAndPropertyConsumerTests(LimitedFixture constructorFixture)
{
    [ClassDataSource<LimitedFixture>(Shared = SharedType.PerClass)]
    public required LimitedFixture Fixture { get; init; }

    [Test]
    public async Task Shared_Instance_Receives_OnTestRegistered_Once()
    {
        var context = TestContext.Current!;

        await Assert.That(Fixture).IsSameReferenceAs(constructorFixture);
        await Assert.That(context.Parallelism.Limiter).IsTypeOf<OneAtATime>();
        await Assert.That(Fixture.RegistrationsFor(context)).IsEqualTo(1);
    }
}

public sealed class PropertySkipTests
{
    [ClassDataSource<SkippingFixture>(Shared = SharedType.PerClass)]
    public required SkippingFixture Fixture { get; init; }

    [Test]
    public void Property_Injected_Fixture_Can_Skip_The_Test()
    {
        throw new InvalidOperationException("This test should have been skipped by its injected fixture.");
    }
}
