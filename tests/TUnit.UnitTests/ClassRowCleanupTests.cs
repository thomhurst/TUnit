using TUnit.Core;
using TUnit.Engine.Building;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.UnitTests;

public class ClassRowCleanupTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task EmptyRefetchedConstructorStateDoesNotCaptureRejectedMethodState(bool removesInheritedKey)
    {
        var data = new object?[] { new object() };
        var row = new TestBuilder.UnclaimedClassRow(data, null, CreateContext());
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse first row"), CreateContext());

        var rejected = CreateContext();
        if (removesInheritedKey)
            rejected.StateBag["removed-by-constructor"] = new object();
        await row.TakeOrFetchAsync(() =>
        {
            // Materialize an empty bag, including the case where the constructor removes its last key.
            rejected.StateBag.TryRemove("removed-by-constructor", out _);
            return Task.FromResult<object?[]?>(data);
        }, rejected);
        rejected.StateBag["method-row"] = new object();
        row.Return(data, null, rejected);

        var compatible = CreateContext();
        var methodValue = new object();
        compatible.StateBag["method-row"] = methodValue;
        compatible.StateBag["removed-by-constructor"] = new object();
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse rejected constructor"), compatible);
        await Assert.That(compatible.StateBag["method-row"]).IsSameReferenceAs(methodValue);
        await Assert.That(compatible.StateBag.ContainsKey("removed-by-constructor")).IsEqualTo(!removesInheritedKey);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RefetchedRejectedConstructorReplaysOnlyItsOwnStateChanges(bool initialState)
    {
        var enumeration = CreateContext();
        if (initialState)
            enumeration.StateBag["method-row"] = new object();
        var original = enumeration with { };
        enumeration.CopyStateBagTo(original);
        var data = new object?[] { new object() };
        var row = new TestBuilder.UnclaimedClassRow(data, null, original, enumeration);
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse first row"), CreateContext());

        var rejected = CreateContext();
        rejected.StateBag["method-row"] = new object();
        rejected.StateBag["removed-by-constructor"] = new object();
        rejected.StateBag["changed-by-constructor"] = new object();
        var constructorValue = new object();
        await row.TakeOrFetchAsync(() =>
        {
            rejected.StateBag["added-by-constructor"] = constructorValue;
            rejected.StateBag["changed-by-constructor"] = constructorValue;
            rejected.StateBag.TryRemove("removed-by-constructor", out _);
            return Task.FromResult<object?[]?>(data);
        }, rejected);
        row.Return(data, null, rejected);

        var compatible = CreateContext();
        var methodValue = new object();
        compatible.StateBag["method-row"] = methodValue;
        compatible.StateBag["removed-by-constructor"] = new object();
        compatible.StateBag["changed-by-constructor"] = new object();
        await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse rejected constructor"), compatible);
        await Assert.That(compatible.StateBag["method-row"]).IsSameReferenceAs(methodValue);
        await Assert.That(compatible.StateBag["added-by-constructor"]).IsSameReferenceAs(constructorValue);
        await Assert.That(compatible.StateBag["changed-by-constructor"]).IsSameReferenceAs(constructorValue);
        await Assert.That(compatible.StateBag.ContainsKey("removed-by-constructor")).IsFalse();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task RejectedMethodRowsRetainCleanupWithoutLifecycleSubscriptions(bool failedRow)
    {
        var original = CreateContext();
        var constructorDisposals = 0;
        var rejectedDisposals = 0;
        var scopeDisposals = 0;
        var rejectedStarts = 0;
        original.Events.OnDispose += (_, _) => { constructorDisposals++; return default; };
        var data = new object?[] { new object() };
        var row = new TestBuilder.UnclaimedClassRow(data, null, original);
        // Two rejected method factories share the saved constructor, but register distinct cleanup.
        for (var i = 0; i < 2; i++)
        {
            var rejected = CreateContext();
            await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse constructor"), rejected);
            var resource = new object();
            rejected.StateBag["rejected-resource"] = resource;
            rejected.Events.OnDispose += async (_, context) =>
            {
                await Assert.That(context.StateBag.Items["rejected-resource"]).IsSameReferenceAs(resource);
                rejectedDisposals++;
            };
            rejected.Events.OnTestStart += (_, _) => { rejectedStarts++; return default; };
            (rejected.Events.DataRowScopes ??= new()).Register(new CallbackScope(() => { scopeDisposals++; return default; }));
            row.Return(data, null, rejected);
        }
        var owner = CreateContext();
        var ownerResource = new object();
        owner.StateBag["rejected-resource"] = ownerResource;
        if (failedRow)
            row.TransferCleanupTo(owner);
        else
            await row.TakeOrFetchAsync(() => throw new InvalidOperationException("Must reuse constructor"), owner);
        var current = TestContext.Current!;
        var executableContext = new TestContext("Cleanup owner", current.ServiceProvider, current.ClassContext,
            owner, CancellationToken.None);
        executableContext.Metadata.TestDetails = current.TestDetails;
        try
        {
            foreach (var handler in owner.Events.OnDispose!.InvocationList)
                await handler.InvokeAsync(executableContext, executableContext);
            if (owner.Events.OnTestStart is { } starts)
                foreach (var handler in starts.InvocationList)
                    await handler.InvokeAsync(executableContext, executableContext);
            await DisposeRow(owner);
            await Assert.That(executableContext.StateBag.Items["rejected-resource"]).IsSameReferenceAs(ownerResource);
        }
        finally
        {
            executableContext.RemoveFromRegistry();
            executableContext.Dispose();
        }
        await Assert.That(constructorDisposals).IsEqualTo(1);
        await Assert.That(rejectedDisposals).IsEqualTo(2);
        await Assert.That(scopeDisposals).IsEqualTo(2);
        await Assert.That(rejectedStarts).IsEqualTo(0);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task InstanceConstructorSubscriptionsReachExecutableRow(bool streaming)
    {
        var current = TestContext.Current!;
        var builder = (ITestBuilder)current.ServiceProvider.GetService(typeof(ITestBuilder))!;
        var metadata = new TestMetadata<ClassRowCleanupTests>
        {
            TestClassType = typeof(ClassRowCleanupTests),
            TestMethodName = nameof(InstanceConstructorSubscriptionsReachExecutableRow),
            TestName = "Lazy constructor event sharing regression",
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [new ClassConstructorAttribute<LateEventConstructor>()],
            MethodMetadata = current.TestDetails.MethodMetadata,
            DataSources = [new ArgumentsAttribute(streaming)],
            ClassDataSources = [],
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
                    tests.Add(test);
            }
            else
            {
                tests.AddRange(await builder.BuildTestsFromMetadataAsync(metadata, new TestBuildingContext(false, null)));
            }
            await Assert.That(tests.Count).IsEqualTo(1);
            var executable = tests[0];
            await executable.CreateInstanceAsync();
            await Assert.That(executable.Context.InternalEventsIfCreated?.OnDispose).IsNotNull();
            foreach (var invocation in executable.Context.InternalEvents.OnDispose!.InvocationList)
                await invocation.InvokeAsync(this, executable.Context);
            await Assert.That(executable.Context.StateBag.Items["constructor-disposals"]).IsEqualTo(1);
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

    public sealed class LateEventConstructor : TUnit.Core.Interfaces.IClassConstructor
    {
        public Task<object> Create(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            ClassConstructorMetadata metadata)
        {
            metadata.TestBuilderContext.Events.OnDispose += (_, context) =>
            {
                context.StateBag.Items["constructor-disposals"] = 1;
                return default;
            };
            return Task.FromResult<object>(new ClassRowCleanupTests());
        }
    }

    [Test]
    public async Task ClaimedRowDoesNotTransferCleanupToLaterSkippedTest()
    {
        var original = CreateContext();
        var disposals = 0;
        (original.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
        {
            disposals++;
            return default;
        }));
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
        (original.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
        {
            disposals++;
            return default;
        }));
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var skipped = CreateContext();
        row.TransferCleanupTo(skipped);
        row.Clear();
        await DisposeRow(skipped);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    public async Task LentRowKeepsScopeUntilSessionCleanupEvenWhenNextFactoryThrows()
    {
        var original = CreateContext();
        var disposals = 0;
        (original.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
        {
            disposals++;
            return default;
        }));
        var row = new TestBuilder.UnclaimedClassRow([new object()], null, original);
        var session = CreateContext();
        row.LendToTemporaryInstance(session);
        var failed = CreateContext();
        await Assert.That(async () => await row.TakeOrFetchAsync(
                () => throw new InvalidOperationException("Factory failure"), failed))
            .Throws<InvalidOperationException>();

        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(0);
        var later = CreateContext();
        row.TransferCleanupTo(later);
        await DisposeRow(later);
        await Assert.That(disposals).IsEqualTo(0);
        await DisposeRow(session);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task EnumerationFailureOnlyDisposesUnclaimedClassScope(bool claimed)
    {
        var enumeration = CreateContext();
        var disposals = 0;
        (enumeration.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
        {
            disposals++;
            return default;
        }));
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
        (factoryContext.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
        {
            disposals++;
            return default;
        }));

        var failed = row.CreateFailureContext(factoryContext);
        await DisposeRow(failed);
        await Assert.That(disposals).IsEqualTo(1);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task ClassIteratorFailureDoesNotDisposeRunnableScope(bool laterSource, bool emptyMethodSource)
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
            DataSources = emptyMethodSource ? [new EmptySource()] : [],
            ClassDataSources = laterSource ? [source, new ThrowingBeforeRowSource()] : [source],
            PropertyDataSources = [],
            InstanceFactory = (_, _) => new ClassRowCleanupTests(),
            InvokeTypedTest = (_, _, _) => ValueTask.CompletedTask
        };

        var tests = (await builder.BuildTestsFromMetadataAsync(metadata, new TestBuildingContext(false, null))).ToArray();
        try
        {
            await Assert.That(tests.Length).IsEqualTo(emptyMethodSource ? 1 : 2);
            await Assert.That(tests[^1].Result!.Exception!.Message).IsEqualTo("Class iterator failure");
            await DisposeTestRow(tests[^1].Context);
            await Assert.That(source.Disposals).IsEqualTo(emptyMethodSource ? 1 : 0);
            if (!emptyMethodSource)
            {
                await DisposeTestRow(tests[0].Context);
            }
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

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task NonRunnableRowsPreservePublicCallbacksAndState(bool factoryThrows)
    {
        var current = TestContext.Current!;
        var builder = (ITestBuilder)current.ServiceProvider.GetService(typeof(ITestBuilder))!;
        var source = new PublicCleanupSource { FactoryThrows = factoryThrows };
        var metadata = new TestMetadata<ClassRowCleanupTests>
        {
            TestClassType = typeof(ClassRowCleanupTests),
            TestMethodName = nameof(NonRunnableRowsPreservePublicCallbacksAndState),
            TestName = "Non-runnable row callbacks regression",
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = current.TestDetails.MethodMetadata,
            DataSources = factoryThrows ? [] : [new EmptySource { SkipIfEmpty = true }],
            ClassDataSources = [source],
            PropertyDataSources = [],
            InstanceFactory = (_, _) => new ClassRowCleanupTests(),
            InvokeTypedTest = (_, _, _) => ValueTask.CompletedTask
        };
        var tests = (await builder.BuildTestsFromMetadataAsync(metadata, new TestBuildingContext(false, null))).ToArray();
        try
        {
            await Assert.That(tests.Length).IsEqualTo(1);
            var context = tests[0].Context;
            await Assert.That(context.StateBag["resource"]).IsSameReferenceAs(source.Resource);
            var events = context.InternalEvents;
            foreach (var callback in new[] { events.OnTestRegistered, events.OnTestSkipped, events.OnTestEnd, events.OnDispose })
            {
                await Assert.That(callback).IsNotNull();
                foreach (var handler in callback!.InvocationList)
                {
                    await handler.InvokeAsync(context, context);
                }
            }
            await Assert.That(source.Callbacks).IsEqualTo(4);
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

    private sealed class PublicCleanupSource : Attribute, IDataSourceAttribute
    {
        public readonly object Resource = new();
        public int Callbacks;
        public bool FactoryThrows;
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }
        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.CompletedTask;
            yield return () =>
            {
                var context = metadata.TestBuilderContext.Current;
                context.StateBag["resource"] = Resource;
                ValueTask Callback(object sender, TestContext testContext)
                {
                    if (!ReferenceEquals(testContext.StateBag["resource"], Resource))
                    {
                        throw new InvalidOperationException("Cleanup lost its owning state");
                    }
                    Callbacks++;
                    return default;
                }
                context.Events.OnTestRegistered += Callback;
                context.Events.OnTestSkipped += Callback;
                context.Events.OnTestEnd += Callback;
                context.Events.OnDispose += Callback;
                if (FactoryThrows) throw new InvalidOperationException("Factory failure");
                return Task.FromResult<object?[]?>([Resource]);
            };
        }
    }

    [Test]
    public async Task ConcurrentTemporaryRowsKeepEverySessionCleanup()
    {
        var session = CreateContext();
        session.Events = new(); // Match the eagerly initialized global session context.
        var contexts = Enumerable.Range(0, 512).Select(_ => CreateContext()).ToArray();
        var disposals = 0;
        foreach (var context in contexts)
        {
            (context.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
            {
                Interlocked.Increment(ref disposals);
                return default;
            }));
        }
        Parallel.ForEach(contexts, context =>
            new TestBuilder.UnclaimedClassRow([new object()], null, context).LendToTemporaryInstance(session));
        await DisposeRow(session);
        await Assert.That(disposals).IsEqualTo(contexts.Length);
    }

    private sealed class EmptySource : Attribute, IDataSourceAttribute
    {
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }
        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.CompletedTask;
            yield break;
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
                (metadata.TestBuilderContext.Current.Events.DataRowScopes ??= new()).Register(new CallbackScope(() =>
                {
                    Disposals++;
                    return default;
                }));
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

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task ClassAndMethodEnumerationStateSurvivesFactories(bool streaming, bool classEnumeration)
    {
        var current = TestContext.Current!;
        var builder = (ITestBuilder)current.ServiceProvider.GetService(typeof(ITestBuilder))!;
        var metadata = new TestMetadata<ClassRowCleanupTests>
        {
            TestClassType = typeof(ClassRowCleanupTests),
            TestMethodName = nameof(ClassAndMethodEnumerationStateSurvivesFactories),
            TestName = "Enumeration state regression",
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = current.TestDetails.MethodMetadata,
            RepeatCount = 1,
            DataSources = [new EnumerationStateSource(false)],
            ClassDataSources = [new EnumerationStateSource(true, classEnumeration)],
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

            await Assert.That(tests.Count).IsEqualTo(4);
            foreach (var test in tests)
            {
                await Assert.That(test.Result?.Exception).IsNull();
                await Assert.That(test.Context.StateBag.ContainsKey("removed-by-method")).IsFalse();
                await Assert.That(test.Context.StateBag.ContainsKey("removed")).IsFalse();
                await Assert.That(test.Context.StateBag.ContainsKey("removed-after-method-update")).IsFalse();
                await Assert.That(test.Context.StateBag.Items["method-overwrite"]).IsEqualTo(test.Context.StateBag.Items["method-enumeration"]);
                await Assert.That(test.Context.StateBag.Items["overwritten"]).IsEqualTo(test.Context.StateBag.Items["class-factory"]);
                if (classEnumeration)
                {
                    await Assert.That(test.Context.StateBag.Items["class-enumeration"]).IsEqualTo(test.Context.StateBag.Items["class-factory"]);
                }
                await Assert.That(test.Context.StateBag.Items["method-enumeration"]).IsEqualTo(test.Context.StateBag.Items["method-factory"]);
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

    private sealed class EnumerationStateSource(bool constructor, bool enumerationState = true) : Attribute, IDataSourceAttribute
    {
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }

        public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
        {
            await Task.CompletedTask;
            var prefix = constructor ? "class" : "method";
            for (var i = 0; i < (constructor ? 2 : 1); i++)
            {
                var value = new object();
                if (enumerationState)
                {
                    metadata.TestBuilderContext.Current.StateBag[prefix + "-enumeration"] = value;
                }
                metadata.TestBuilderContext.Current.StateBag["method-overwrite"] = value;
                metadata.TestBuilderContext.Current.StateBag["removed-after-method-update"] = value;
                if (constructor)
                {
                    metadata.TestBuilderContext.Current.StateBag["overwritten"] = new object();
                    metadata.TestBuilderContext.Current.StateBag["removed"] = new object();
                    metadata.TestBuilderContext.Current.StateBag["removed-by-method"] = new object();
                }
                else
                {
                    metadata.TestBuilderContext.Current.StateBag.TryRemove("removed-by-method", out _);
                }
                yield return () =>
                {
                    var state = metadata.TestBuilderContext.Current.StateBag;
                    if (enumerationState && !ReferenceEquals(state[prefix + "-enumeration"], value))
                    {
                        throw new InvalidOperationException("Enumeration state belongs to another row");
                    }
                    if (!state.TryAdd(prefix + "-factory", value))
                    {
                        throw new InvalidOperationException("Factory state leaked into enumeration");
                    }
                    if (constructor)
                    {
                        state["overwritten"] = value;
                        state.TryRemove("removed", out _);
                        state.TryRemove("removed-after-method-update", out _);
                    }
                    return Task.FromResult<object?[]?>([value]);
                };
                if (metadata.TestBuilderContext.Current.StateBag.ContainsKey(prefix + "-factory"))
                {
                    throw new InvalidOperationException("Factory state leaked when enumeration resumed");
                }
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
        if (context.InternalEventsIfCreated?.DataRowScopes is { } scopes &&
            await scopes.CompleteAsync() is { } exceptions)
        {
            throw new AggregateException(exceptions);
        }
    }

    private static TestBuilderContext CreateContext() => new()
    {
        TestMetadata = TestContext.Current!.TestDetails.MethodMetadata
    };

    private static async Task DisposeRow(TestBuilderContext context)
    {
        if (context.EventsIfCreated?.DataRowScopes is { } scopes &&
            await scopes.CompleteAsync() is { } exceptions)
        {
            throw new AggregateException(exceptions);
        }
    }

    private sealed class CallbackScope(Func<ValueTask> dispose) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => dispose();
    }
}
