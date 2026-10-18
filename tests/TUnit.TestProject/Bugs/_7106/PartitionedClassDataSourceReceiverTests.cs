using System.Collections.Concurrent;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// A custom class-level data source whose rows infer different type arguments is split per inferred
// type in reflection mode. Its registration callback still runs for every test it produces, on the
// source with its properties injected (#7106).
// Source generation cannot infer type arguments from an untyped runtime data source, so these classes
// are only discovered in reflection mode.
[EngineTest(ExpectedResult.Pass)]
[MixedTypeRowsWithRegistrationReceiver]
public class PartitionedClassDataSourceReceiver<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(TestContext.Current!.StateBag.Items[MixedTypeRowsWithRegistrationReceiverAttribute.Key]).IsEqualTo(true);
    }
}

// Discovery reads a class data source's rows to infer the type arguments. Tests receive those rows,
// so the source is not read a second time, here a source that hands out each queued value once.
[EngineTest(ExpectedResult.Pass)]
[QueueRowsWithRegistrationReceiver]
public class SingleTypeClassDataSourceReadOnce<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value is 1 or 2).IsTrue();
        await Assert.That(TestContext.Current!.StateBag.Items.ContainsKey(QueueRowsWithRegistrationReceiverAttribute.Key)).IsTrue();
    }
}

// A deferred class data source keeps its deferral once discovery has read its rows to infer the type
// arguments: discovery shows one placeholder, and the rows expand at runtime.
[EngineTest(ExpectedResult.Pass)]
[DeferredRowsWithRegistrationReceiver(DeferEnumeration = true)]
public class DeferredClassDataSourceReceiver<T>(T value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value).IsTypeOf<T>();
        await Assert.That(TestContext.Current!.StateBag.Items.ContainsKey(DeferredRowsWithRegistrationReceiverAttribute.Key)).IsTrue();
    }
}

public sealed class Issue7106ReceiverDependency;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DeferredRowsWithRegistrationReceiverAttribute : UntypedDataSourceGeneratorAttribute, ITestRegisteredEventReceiver
{
    public const string Key = "Issue7106DeferredReceiver";

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [1];
        yield return () => [2];
        yield return () => [3];
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag[Key] = true;
        return default;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class MixedTypeRowsWithRegistrationReceiverAttribute : UntypedDataSourceGeneratorAttribute, ITestRegisteredEventReceiver
{
    public const string Key = "Issue7106PartitionedReceiver";

    [ClassDataSource<Issue7106ReceiverDependency>(Shared = SharedType.None)]
    public Issue7106ReceiverDependency? Dependency { get; set; }

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => [1];
        yield return () => ["a"];
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        // Records whether the source had its properties injected before its callback ran
        context.StateBag[Key] = Dependency is not null;
        return default;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class QueueRowsWithRegistrationReceiverAttribute : UntypedDataSourceGeneratorAttribute, ITestRegisteredEventReceiver
{
    public const string Key = "Issue7106QueueReceiver";

    private static readonly ConcurrentQueue<int> Values = new([1, 2]);

    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Each value is handed out once: a second read of this source yields nothing
        while (Values.TryDequeue(out var value))
        {
            yield return () => [value];
        }
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.StateBag[Key] = true;
        return default;
    }
}
