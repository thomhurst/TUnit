using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.UnitTests;

public class ClassRowCleanupTests
{
    [Test]
    public async Task ClaimedRowDoesNotTransferCleanupToLaterSkippedTest()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var runnable = CreateContext();
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must use saved row"), runnable);

        var skipped = CreateContext();
        row.TransferCleanupTo(skipped);
        await DisposeRow(skipped);
        await Assert.That(disposals).IsEqualTo(0);
        await DisposeRow(runnable);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task UnclaimedRowTransfersCleanupToSkippedTest()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var skipped = CreateContext();
        row.TransferCleanupTo(skipped);
        row.Clear();
        await DisposeRow(skipped);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task LentRowTransfersCleanupEvenWhenNextFactoryThrows()
    {
        var original = CreateContext();
        var disposals = 0;
        original.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        row.LendToTemporaryInstance();
        var failed = CreateContext();
        await Assert.That(async () => await row.TakeOrFetchAsync(
                () => throw new InvalidOperationException("Factory failure"), failed))
            .Throws<InvalidOperationException>();

        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(1);
        var later = CreateContext();
        row.TransferCleanupTo(later);
        await DisposeRow(later);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task EnumerationFailureOnlyDisposesUnclaimedClassScope(bool claimed)
    {
        var enumeration = CreateContext();
        var disposals = 0;
        enumeration.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, enumeration);
        var runnable = CreateContext();
        if (claimed)
        {
            await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must use saved row"), runnable);
        }

        // MoveNext throws after yielding a row (or before yielding any rows).
        var failed = row.CreateFailureContext(enumeration);
        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(claimed ? 0 : 1);
        await DisposeRow(runnable);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task ClassEnumerationFailurePreservesSeparateFactoryCleanup()
    {
        var enumeration = CreateContext();
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, enumeration);
        var factoryContext = CreateContext();
        var disposals = 0;
        factoryContext.Events.OnDataRowDispose += (_, _) =>
        {
            disposals++;
            return default;
        };

        var failed = row.CreateFailureContext(factoryContext);
        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ClassIteratorFailureDoesNotDisposeRunnableScope(bool laterSource)
    {
        var current = TestContext.Current!;
        var builder = (ITestBuilder)current.ServiceProvider.GetService(typeof(ITestBuilder))!;
        var source = new ThrowingClassSource { ThrowAfterRow = !laterSource };
        var metadata = new TestMetadata<ClassRowCleanupTests>
        {
            TestClassType = typeof(ClassRowCleanupTests),
            TestMethodName = nameof(ClassIteratorFailureDoesNotDisposeRunnableScope),
            TestName = "Class iterator failure regression",
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = current.TestDetails.MethodMetadata,
            DataSources = [],
            ClassDataSources = laterSource ? [source, new ThrowingBeforeRowSource()] : [source],
            PropertyDataSources = [],
            InstanceFactory = (_, _) => new ClassRowCleanupTests(),
            InvokeTypedTest = (_, _, _) => ValueTask.CompletedTask
        };

        var tests = (await builder.BuildTestsFromMetadataAsync(metadata, new TestBuildingContext(false, null))).ToArray();
        try
        {
            await Assert.That(tests.Length).IsEqualTo(2);
            await Assert.That(tests[1].Result!.Exception!.Message).IsEqualTo("Class iterator failure");
            await DisposeTestRow(tests[1].Context);
            await Assert.That(source.Disposals).IsEqualTo(0);
            await DisposeTestRow(tests[0].Context);
            await Assert.That(source.Disposals).IsEqualTo(1);
        }
        finally
        {
            foreach (var test in tests)
            {
                test.Context.RemoveFromRegistry();
                test.Context.Dispose();
            }
        }
    }

    private sealed class ThrowingClassSource : Attribute, IDataSourceAttribute
    {
        public int Disposals;
        public bool ThrowAfterRow = true;
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }

        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.CompletedTask;
            yield return () =>
            {
                metadata.TestBuilderContext.Current.Events.OnDataRowDispose += (_, _) =>
                {
                    Disposals++;
                    return default;
                };
                return Task.FromResult<object?[]?>([new object()]);
            };
            if (ThrowAfterRow)
            {
                throw new InvalidOperationException("Class iterator failure");
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ConstructorFactoryStateIsIsolatedAcrossRepeats(bool streaming)
    {
        var current = TestContext.Current!;
        var builder = (ITestBuilder)current.ServiceProvider.GetService(typeof(ITestBuilder))!;
        var source = new StatefulConstructorSource();
        var metadata = new TestMetadata<ClassRowCleanupTests>
        {
            TestClassType = typeof(ClassRowCleanupTests),
            TestMethodName = nameof(ConstructorFactoryStateIsIsolatedAcrossRepeats),
            TestName = "Constructor factory state regression",
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = current.TestDetails.MethodMetadata,
            RepeatCount = 2,
            DataSources = [],
            ClassDataSources = [source],
            PropertyDataSources = [],
            InstanceFactory = (_, _) => new ClassRowCleanupTests(),
            InvokeTypedTest = (_, _, _) => ValueTask.CompletedTask
        };
        var tests = new List<AbstractExecutableTest>();
        try
        {
            if (streaming)
            {
                await foreach (var test in builder.BuildTestsStreamingAsync(metadata))
                {
                    tests.Add(test);
                }
            }
            else
            {
                tests.AddRange(await builder.BuildTestsFromMetadataAsync(metadata, new TestBuildingContext(false, null)));
            }
            await Assert.That(tests.Count).IsEqualTo(3);
            await Assert.That(source.Resources.Count).IsEqualTo(3);
            for (var i = 0; i < tests.Count; i++)
            {
                await Assert.That(tests[i].Result?.Exception).IsNull();
                await Assert.That(ReferenceEquals(tests[i].Context.StateBag.Items["resource"], source.Resources[i])).IsTrue();
            }
        }
        finally
        {
            foreach (var test in tests)
            {
                test.Context.RemoveFromRegistry();
                test.Context.Dispose();
            }
        }
    }

    private sealed class StatefulConstructorSource : Attribute, IDataSourceAttribute
    {
        public readonly List<object> Resources = [];
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }

        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.CompletedTask;
            yield return () =>
            {
                var resource = new object();
                if (!metadata.TestBuilderContext.Current.StateBag.TryAdd("resource", resource))
                {
                    throw new InvalidOperationException("Constructor factory inherited another row's resource");
                }
                Resources.Add(resource);
                return Task.FromResult<object?[]?>([resource]);
            };
        }
    }

    private sealed class ThrowingBeforeRowSource : Attribute, IDataSourceAttribute
    {
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }

        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.FromException(new InvalidOperationException("Class iterator failure"));
            yield break;
        }
    }

    private static async Task DisposeTestRow(TestContext context)
    {
        if (context.InternalEventsIfCreated?.OnDataRowDispose is { } cleanup)
        {
            foreach (var handler in cleanup.InvocationList)
            {
                await handler.InvokeAsync(context, (context, null));
            }
        }
    }

    private static TestBuilderContext CreateContext() => new()
    {
        TestMetadata = TestContext.Current!.TestDetails.MethodMetadata
    };

    private static async Task DisposeRow(TestBuilderContext context)
    {
        if (context.EventsIfCreated?.OnDataRowDispose is { } cleanup)
        {
            foreach (var handler in cleanup.InvocationList)
            {
                await handler.InvokeAsync(TestContext.Current!, (TestContext.Current!, null));
            }
        }
    }
}
