using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Building;

internal sealed class TestBuilder : ITestBuilder
{
    private readonly string _sessionId;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly IContextProvider _contextProvider;
    private readonly ObjectLifecycleService _objectLifecycleService;
    private readonly Discovery.IHookRegistrar _hookDiscoveryService;
    private readonly IMetadataFilterMatcher _filterMatcher;

    public TestBuilder(
        string sessionId,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        IContextProvider contextProvider,
        ObjectLifecycleService objectLifecycleService,
        Discovery.IHookRegistrar hookDiscoveryService,
        IMetadataFilterMatcher filterMatcher)
    {
        _sessionId = sessionId;
        _hookDiscoveryService = hookDiscoveryService;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _contextProvider = contextProvider;
        _objectLifecycleService = objectLifecycleService;
        _filterMatcher = filterMatcher ?? throw new ArgumentNullException(nameof(filterMatcher));
    }

    /// <summary>
    /// Initializes class data objects during test building.
    /// Only IAsyncDiscoveryInitializer objects are initialized during discovery.
    /// Regular IAsyncInitializer objects are deferred to execution phase.
    /// </summary>
    private static async Task InitializeClassDataAsync(object?[] classData)
    {
        if (classData == null || classData.Length == 0)
        {
            return;
        }

        foreach (var data in classData)
        {
            // Discovery: only IAsyncDiscoveryInitializer objects are initialized.
            // Regular IAsyncInitializer objects are deferred to execution phase.
            await ObjectInitializer.InitializeForDiscoveryAsync(data);
        }
    }

    private async Task<object> CreateInstance(TestMetadata metadata, Type[] resolvedClassGenericArgs, object?[] classData, TestBuilderContext builderContext)
    {
        foreach (var data in classData)
        {
            await _objectLifecycleService.InitializeObjectForExecutionAsync(data);
        }

        // First try to create instance with ClassConstructor attribute
        // Use attributes from context if available
        var attributes = builderContext.InitializedAttributes ?? metadata.GetOrCreateAttributes();

        var instance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
            attributes,
            metadata.TestClassType,
            builderContext,
            metadata.TestSessionId);

        if (instance != null)
        {
            return instance;
        }

        // Fall back to InstanceFactory if no ClassConstructor or it returned null
        if (metadata.InstanceFactory == null)
        {
            throw new InvalidOperationException($"No instance factory or class constructor available for {metadata.TestClassType.FullName}.");
        }

        try
        {
            instance = metadata.InstanceFactory(resolvedClassGenericArgs, classData);
            if (instance is null)
            {
                throw new InvalidOperationException($"Error creating test class instance for {metadata.TestClassType.FullName}.");
            }
            return instance;
        }
        catch (NotSupportedException ex)
        {
            // This can happen when:
            // 1. ClassConstructor is present but wasn't found or returned null
            // 2. AOT scenarios where generic type instantiation fails
            if (ex.Message.Contains("missing native code or metadata"))
            {
                throw new InvalidOperationException($"Failed to create instance of {metadata.TestClassType.FullName} in AOT scenario. Ensure types are properly annotated for AOT compatibility.", ex);
            }
            throw new InvalidOperationException($"Failed to create instance of {metadata.TestClassType.FullName}. This may be due to a ClassConstructor attribute issue or AOT incompatibility.", ex);
        }
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    public async ValueTask<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata, TestBuildingContext buildingContext, CancellationToken cancellationToken = default)
    {
        // OPTIMIZATION: Pre-filter in execution mode to skip building tests that cannot match the filter
        if (buildingContext.IsForExecution && buildingContext.Filter != null)
        {
            if (!CouldTestMatchFilter(buildingContext.Filter, metadata))
            {
                // This test class cannot match the filter - skip all expensive work!
                return [];
            }
        }

        var tests = new List<AbstractExecutableTest>();

        try
        {
            // Create a context for capturing output during test building.
            // Leaving this async method discards the AsyncLocal write, so disposal does not need to
            // reset Current (the catch block below clears it explicitly for the failure path).
            var buildContext = new TestBuildContext();
            using var buildContextLease = new BuildContextLease(buildContext);
            TestBuildContext.Current = buildContext;

            // Handle GenericTestMetadata with ConcreteInstantiations
            if (metadata is GenericTestMetadata { ConcreteInstantiations.Count: > 0 } genericMetadata)
            {
                // Build tests from each concrete instantiation
                foreach (var concreteMetadata in genericMetadata.ConcreteInstantiations.Values)
                {
                    var concreteTests = await BuildTestsFromMetadataAsync(concreteMetadata, buildingContext, cancellationToken);
                    tests.AddRange(concreteTests);
                }
                return tests;
            }


            // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
            var repeatCount = metadata.RepeatCount ?? 0;

            // Create and initialize attributes ONCE
            var attributes = await InitializeAttributesAsync(metadata.GetOrCreateAttributes(), cancellationToken);

            if (HasInstanceDataAccessor(metadata.ClassDataSources))
            {
                var failedTest = CreateFailedTestForClassDataSourceCircularDependency(metadata);
                tests.Add(failedTest);
                return tests;
            }

            // Create a single context accessor that we'll reuse, updating its Current property for each test
            // StateBag and Events are lazy-initialized, so we don't need to pre-allocate them
            var testBuilderContext = new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                InitializedAttributes = attributes  // Store the initialized attributes
            };

            var classConstructorAttribute = attributes.FirstOfType<ClassConstructorAttribute>();

            // Without data sources or a class constructor no user code runs while building, so nothing
            // can observe TestBuilderContext.Current — skip publishing it (each AsyncLocal write
            // allocates a new ExecutionContext and value map, twice per test here).
            var publishBuilderContext = metadata.ClassDataSources.Length > 0
                || metadata.DataSources.Length > 0
                || classConstructorAttribute != null;

            // Set the static AsyncLocal immediately so it's available for property data sources
            // This must be set BEFORE any operations that might invoke data source methods
            if (publishBuilderContext)
            {
                TestBuilderContext.Current = testBuilderContext;
            }

            // Check for ClassConstructor attribute and set it early if present (reuse already created attributes)
            if (classConstructorAttribute != null)
            {
                testBuilderContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
            }

            var contextAccessor = new TestBuilderContextAccessor(testBuilderContext, publishBuilderContext);

            // DeferEnumeration: emit a single placeholder node instead of enumerating the data source(s).
            // The placeholder is built during both discovery and the execution build (so it matches the
            // IDE's UID filter); DeferredTestExpander later re-runs this method with IgnoreDeferral set to
            // produce the real cases. Skipped entirely when IgnoreDeferral is set (the expansion pass).
            if (!buildingContext.IgnoreDeferral && HasDeferredDataSource(metadata))
            {
                tests.Add(await BuildDeferredPlaceholderAsync(metadata, testBuilderContext, cancellationToken));
                return tests;
            }

            var classDataAttributeIndex = 0;
            foreach (var classDataSource in await GetDataSourcesAsync(metadata.ClassDataSources, cancellationToken))
            {
                classDataAttributeIndex++;

                var classDataLoopIndex = 0;
                var hasAnyClassData = false;
                await foreach (var classDataFactory in GetInitializedDataRowsAsync(
                    classDataSource,
                    metadata,
                    generatorType: DataGeneratorType.ClassParameters,
                    testClassInstance: null,
                    classInstanceArguments: null,
                    contextAccessor,
                    cancellationToken))
                {
                    hasAnyClassData = true;
                    classDataLoopIndex++;

                    var (classData, firstClassRowMetadata, firstClassRowEvents) = await FetchFirstClassRowAsync(
                        classDataFactory, classDataSource, metadata, contextAccessor);

                    // No test holds this first fetch yet. The first test built from this class row takes it
                    // instead of calling the factory again, so a non-shared source does not create an extra
                    // instance that nothing tracks or disposes (#7157).
                    var unclaimedClassRow = new UnclaimedClassRow(classData, firstClassRowMetadata, firstClassRowEvents);

#if NET
                    // Register scope for class data objects from the simple path.
                    // Also registered in the repeat loop below for re-fetched instances.
                    TraceScopeRegistry.RegisterFromDataSource(classDataSource, classData);
#endif

                    // Initialize objects before method data sources are evaluated.
                    // ObjectInitializer is phase-aware and will only initialize IAsyncDiscoveryInitializer during Discovery.
                    await InitializeClassDataAsync(classData);

                    var needsInstanceForMethodDataSources = HasInstanceDataAccessor(metadata.DataSources);

                    object? instanceForMethodDataSources = null;
                    try
                    {
                        if (needsInstanceForMethodDataSources)
                        {
                            // The temporary instance is built from the first fetch and disposed below, so no
                            // test may receive those objects: it could find them disposed. The next test
                            // tracks them instead, so they are still disposed.
                            unclaimedClassRow.LendToTemporaryInstance();

                            var instanceResult = await CreateInitializedInstanceForMethodDataSources(
                                metadata,
                                classDataAttributeIndex,
                                classDataLoopIndex,
                                classData,
                                testBuilderContext,
                                cancellationToken);

                            if (!instanceResult.Success)
                            {
                                var failedTest = CreateFailedTestForInstanceDataSourceError(metadata, instanceResult.Exception!, classData, unclaimedClassRow.TakeEvents());
                                tests.Add(failedTest);
                                continue;
                            }

                            instanceForMethodDataSources = instanceResult.Instance;
                        }

                        var methodDataAttributeIndex = 0;
                        foreach (var methodDataSource in await GetDataSourcesAsync(metadata.DataSources, cancellationToken))
                        {
                            methodDataAttributeIndex++;

                            var methodDataLoopIndex = 0;
                            var hasAnyMethodData = false;
                            await foreach (var methodDataFactory in GetInitializedDataRowsAsync(
                                methodDataSource,
                                metadata,
                                generatorType: DataGeneratorType.TestParameters,
                                testClassInstance: methodDataSource is IAccessesInstanceData ? instanceForMethodDataSources : null,
                                classInstanceArguments: classData,
                                contextAccessor,
                                cancellationToken))
                            {
                                hasAnyMethodData = true;
                                methodDataLoopIndex++;

                                for (var i = 0; i < repeatCount + 1; i++)
                                {
                                    // Update context BEFORE calling data factories so they track objects in the right context
                                    // StateBag and Events are lazy-initialized for performance
                                    contextAccessor.Current = new TestBuilderContext
                                    {
                                        TestMetadata = metadata.MethodMetadata,
                                        DataSourceAttribute = methodDataSource,
                                        ClassDataSourceAttribute = classDataSource,
                                        InitializedAttributes = testBuilderContext.InitializedAttributes,
                                        ClassConstructor = testBuilderContext.ClassConstructor
                                    };

                                    testBuilderContext.CopyStateBagTo(contextAccessor.Current);

                                    var (classDataUnwrapped, classRowMetadata) = await unclaimedClassRow.TakeOrFetchAsync(classDataFactory, contextAccessor.Current);
                                    classData = classDataUnwrapped;

                                    object?[] methodRow;
                                    try
                                    {
                                        methodRow = await methodDataFactory() ?? [];
                                    }
                                    catch (GenericTypeResolutionException ex) when (methodDataSource is GenericConstraintViolationDataSource)
                                    {
                                        // A source-generated row whose inferred type arguments break the method's
                                        // constraints: fail this row only, as reflection mode does when it resolves
                                        // the row's type arguments, and keep building the remaining rows.
                                        var (reportedException, rowData) = UnpackConstraintViolation(ex, metadata.MethodMetadata.Parameters);
                                        tests.Add(CreateFailedTestForDataRowError(metadata, reportedException, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, i, unclaimedClassRow.WithLentObjects(classData), rowData, contextAccessor.Current.EventsIfCreated));
                                        continue;
                                    }

                                    var (methodData, methodRowMetadata) = DataUnwrapper.UnwrapWithTypesAndMetadata(methodRow, metadata.MethodMetadata.Parameters);

#if NET
                                // Re-register: classDataFactory() may have been called again above and
                                // returned a different instance for non-shared objects. First-registration-wins
                                // semantics make this a no-op for the same instance.
                                TraceScopeRegistry.RegisterFromDataSource(classDataSource, classData);
                                TraceScopeRegistry.RegisterFromDataSource(methodDataSource, methodData);
#endif

                                    // Extract and merge metadata from data source attributes and TestDataRow wrappers
                                    var classAttrMetadata = DataSourceMetadataExtractor.ExtractFromAttribute(classDataSource);
                                    var methodAttrMetadata = DataSourceMetadataExtractor.ExtractFromAttribute(methodDataSource);
                                    var mergedClassMetadata = DataSourceMetadataExtractor.Merge(classRowMetadata, classAttrMetadata);
                                    var mergedMethodMetadata = DataSourceMetadataExtractor.Merge(methodRowMetadata, methodAttrMetadata);
                                    var finalMetadata = DataSourceMetadataExtractor.Merge(mergedMethodMetadata, mergedClassMetadata);

                                    // Initialize method data objects (ObjectInitializer is phase-aware)
                                    await InitializeClassDataAsync(methodData);

                                    // For concrete generic instantiations, check if the data is compatible with the expected types
                                    if (metadata.GenericMethodTypeArguments is { Length: > 0 })
                                    {

                                        if (!IsDataCompatibleWithExpectedTypes(metadata, methodData))
                                        {
                                            // Skip this data source as it's not compatible with the expected types.
                                            // No test holds the class row's objects, so the next test takes them,
                                            // with the events subscribed while this row was fetched.
                                            unclaimedClassRow.Return(classData, classRowMetadata, contextAccessor.Current.EventsIfCreated);
                                            continue;
                                        }
                                    }

                                    Type[] resolvedClassGenericArgs;
                                    Type[] resolvedMethodGenericArgs;

                                    if (metadata.GenericTypeInfo == null
                                        && metadata.GenericMethodTypeArguments is not { Length: > 0 }
                                        && metadata.GenericMethodInfo == null)
                                    {
                                        // Nothing generic to resolve (TestGenericTypeResolver.Resolve would return empty
                                        // arrays) — skip the placeholder TestData and resolution result per test.
                                        resolvedClassGenericArgs = Type.EmptyTypes;
                                        resolvedMethodGenericArgs = Type.EmptyTypes;
                                    }
                                    else
                                    {
                                        var tempTestData = new TestData
                                        {
                                            TestClassInstanceFactory = () => Task.FromResult<object>(null!), // Temporary placeholder
                                            ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                            ClassDataLoopIndex = classDataLoopIndex,
                                            ClassData = classData,
                                            MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                            MethodDataLoopIndex = methodDataLoopIndex,
                                            MethodData = methodData,
                                            RepeatIndex = i,
                                            InheritanceDepth = metadata.InheritanceDepth
                                        };

                                        try
                                        {
                                            var resolution = TestGenericTypeResolver.Resolve(metadata, tempTestData);
                                            resolvedClassGenericArgs = resolution.ResolvedClassGenericArguments;
                                            resolvedMethodGenericArgs = resolution.ResolvedMethodGenericArguments;
                                        }
                                        catch (GenericTypeResolutionException) when (
                                            metadata.TestClassType.IsGenericTypeDefinition &&
                                            classData.Length == 0 &&
                                            methodData.Length > 0)
                                        {
                                            // Special handling for generic classes with no constructor arguments
                                            // but with method parameters that can help infer the generic types
                                            try
                                            {
                                                resolvedClassGenericArgs = TryInferClassGenericsFromMethodData(
                                                    metadata, methodData);
                                                resolvedMethodGenericArgs = Type.EmptyTypes; // No method generics in this case
                                            }
                                            catch (Exception innerEx)
                                            {
                                                // If we still can't resolve, create a failed test
                                                var failedTest = CreateFailedTestForDataRowError(metadata, innerEx, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, i, unclaimedClassRow.WithLentObjects(classData), methodData, contextAccessor.Current.EventsIfCreated);
                                                tests.Add(failedTest);
                                                continue;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // If generic resolution fails, create a failed test
                                            var failedTest = CreateFailedTestForDataRowError(metadata, ex, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, i, unclaimedClassRow.WithLentObjects(classData), methodData, contextAccessor.Current.EventsIfCreated);
                                            tests.Add(failedTest);
                                            continue;
                                        }
                                    }

                                    if (metadata.TestClassType.IsGenericTypeDefinition && resolvedClassGenericArgs.Length == 0)
                                    {
                                        throw new InvalidOperationException($"Cannot create test for generic class '{metadata.TestClassType.Name}': No type arguments could be inferred. Add [GenerateGenericTest<ConcreteType>] to the class, or use a data source (like [ClassDataSource<T>] or [Arguments]) that provides constructor arguments to infer the generic type arguments from.");
                                    }

                                    var basicSkipReason = GetBasicSkipReason(metadata, attributes);

                                    Func<Task<object>> instanceFactory;

                                    if (basicSkipReason is { Length: > 0 })
                                    {
                                        instanceFactory = () => Task.FromResult<object>(SkippedTestInstance.Instance);
                                    }
                                    else
                                    {
                                        var capturedMetadata = metadata;
                                        var capturedClassGenericArgs = resolvedClassGenericArgs;
                                        var capturedClassData = classData;
                                        var capturedContext = contextAccessor.Current;
                                        instanceFactory = () => CreateInstance(capturedMetadata, capturedClassGenericArgs, capturedClassData, capturedContext);
                                    }

                                    var testData = new TestData
                                    {
                                        TestClassInstanceFactory = instanceFactory,
                                        ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                        ClassDataLoopIndex = classDataLoopIndex,
                                        ClassData = classData,
                                        MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                        MethodDataLoopIndex = methodDataLoopIndex,
                                        MethodData = methodData,
                                        RepeatIndex = i,
                                        InheritanceDepth = metadata.InheritanceDepth,
                                        ResolvedClassGenericArguments = resolvedClassGenericArgs,
                                        ResolvedMethodGenericArguments = resolvedMethodGenericArgs,
                                        Metadata = finalMetadata,
                                        UnreceivedClassRowObjects = unclaimedClassRow.TakeLentObjects()
                                    };

                                    TestBuilderContext testSpecificContext;
                                    if (classDataSource is NoDataSource && methodDataSource is NoDataSource)
                                    {
                                        // No data source ran against the per-iteration context (NoDataSource has no
                                        // factories that could register events or state), and it already holds
                                        // exactly the values below — use it directly rather than cloning it.
                                        testSpecificContext = contextAccessor.Current;
                                    }
                                    else
                                    {
                                        // Events is lazy-initialized; explicitly share StateBag from per-iteration context
                                        testSpecificContext = new TestBuilderContext
                                        {
                                            TestMetadata = metadata.MethodMetadata,
                                            ClassConstructor = testBuilderContext.ClassConstructor,
                                            DataSourceAttribute = contextAccessor.Current.DataSourceAttribute,
                                            ClassDataSourceAttribute = classDataSource,
                                            InitializedAttributes = attributes
                                        };

                                        // Shares the per-iteration bag lazily; it is only allocated if something reads it.
                                        testSpecificContext.ShareStateBagWith(contextAccessor.Current);

                                        // The data sources subscribed on the per-iteration context while this row was
                                        // fetched (a DI scope's disposal, for example); the test raises them (#7205).
                                        testSpecificContext.ShareEventsWith(contextAccessor.Current);
                                    }

                                    var test = await BuildTestAsync(metadata, testData, testSpecificContext, cancellationToken);

                                    // If we have a basic skip reason, set it immediately
                                    if (!string.IsNullOrEmpty(basicSkipReason))
                                    {
                                        test.Context.SkipReason = basicSkipReason;
                                    }
                                    tests.Add(test);

                                    // Context already updated at the beginning of the loop before calling factories
                                }
                            }

                            // If no data was yielded and SkipIfEmpty is true, create a skipped test
                            if (!hasAnyMethodData && methodDataSource.SkipIfEmpty)
                            {
                                const string skipReason = "Data source returned no data";

                                // The test built below holds classData, which may be the unclaimed first fetch,
                                // and tracks any objects that a temporary instance used.
                                var lentClassRowObjects = unclaimedClassRow.TakeLentObjects();
                                var classRowEvents = unclaimedClassRow.TakeEvents();
                                unclaimedClassRow.Clear();

                                Type[] resolvedClassGenericArgs;
                                Exception? genericResolutionException = null;
                                try
                                {
                                    resolvedClassGenericArgs = metadata.TestClassType.IsGenericTypeDefinition
                                        ? TryInferClassGenericsFromDataSources(metadata)
                                        : Type.EmptyTypes;
                                }
                                catch (Exception ex)
                                {
                                    resolvedClassGenericArgs = Type.EmptyTypes;
                                    genericResolutionException = ex;
                                }

                                // If generic type inference failed, create a failed test instead of skipped
                                if (genericResolutionException != null)
                                {
                                    var failedTest = CreateFailedTestForDataGenerationError(metadata, genericResolutionException, new TestDataCombination(), classData, lentClassRowObjects, classRowEvents);
                                    tests.Add(failedTest);
                                }
                                else
                                {
                                    var testData = new TestData
                                    {
                                        TestClassInstanceFactory = () => Task.FromResult<object>(SkippedTestInstance.Instance),
                                        ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                        ClassDataLoopIndex = classDataLoopIndex,
                                        ClassData = classData,
                                        MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                        MethodDataLoopIndex = 1, // Use 1 since we're creating a single skipped test
                                        MethodData = [],
                                        RepeatIndex = 0,
                                        InheritanceDepth = metadata.InheritanceDepth,
                                        ResolvedClassGenericArguments = resolvedClassGenericArgs,
                                        ResolvedMethodGenericArguments = Type.EmptyTypes,
                                        UnreceivedClassRowObjects = lentClassRowObjects
                                    };

                                    var testSpecificContext = new TestBuilderContext
                                    {
                                        TestMetadata = metadata.MethodMetadata,
                                        ClassConstructor = testBuilderContext.ClassConstructor,
                                        DataSourceAttribute = methodDataSource,
                                        ClassDataSourceAttribute = classDataSource,
                                        InitializedAttributes = attributes
                                    };

                                    testBuilderContext.CopyStateBagTo(testSpecificContext);

                                    if (classRowEvents is not null)
                                    {
                                        testSpecificContext.Events = classRowEvents;
                                    }

                                    var test = await BuildTestAsync(metadata, testData, testSpecificContext, cancellationToken: cancellationToken);
                                    test.Context.SkipReason = skipReason;
                                    tests.Add(test);
                                }
                            }
                        }
                    }
                    finally
                    {
                        await DisposeInstanceForMethodDataSourcesAsync(instanceForMethodDataSources);
                    }
                }

                // If no class data was yielded and SkipIfEmpty is true, create a skipped test
                if (!hasAnyClassData && classDataSource.SkipIfEmpty)
                {
                    const string skipReason = "Data source returned no data";

                    Type[] resolvedClassGenericArgs;
                    Exception? genericResolutionException = null;
                    try
                    {
                        resolvedClassGenericArgs = metadata.TestClassType.IsGenericTypeDefinition
                            ? TryInferClassGenericsFromDataSources(metadata)
                            : Type.EmptyTypes;
                    }
                    catch (Exception ex)
                    {
                        resolvedClassGenericArgs = Type.EmptyTypes;
                        genericResolutionException = ex;
                    }

                    // If generic type inference failed, create a failed test instead of skipped
                    if (genericResolutionException != null)
                    {
                        var failedTest = CreateFailedTestForDataGenerationError(metadata, genericResolutionException);
                        tests.Add(failedTest);
                    }
                    else
                    {
                        var testData = new TestData
                        {
                            TestClassInstanceFactory = () => Task.FromResult<object>(SkippedTestInstance.Instance),
                            ClassDataSourceAttributeIndex = classDataAttributeIndex,
                            ClassDataLoopIndex = 1, // Use 1 since we're creating a single skipped test
                            ClassData = [],
                            MethodDataSourceAttributeIndex = 0,
                            MethodDataLoopIndex = 0,
                            MethodData = [],
                            RepeatIndex = 0,
                            InheritanceDepth = metadata.InheritanceDepth,
                            ResolvedClassGenericArguments = resolvedClassGenericArgs,
                            ResolvedMethodGenericArguments = Type.EmptyTypes
                        };

                        // The class data source yielded no rows, so no method data source was ever
                        // enumerated — only ClassDataSourceAttribute is populated. The class attribute
                        // is already present in the initialized attributes (it is declared on the class),
                        // so nothing is lost by leaving the method slot empty.
                        var testSpecificContext = new TestBuilderContext
                        {
                            TestMetadata = metadata.MethodMetadata,
                            ClassConstructor = testBuilderContext.ClassConstructor,
                            ClassDataSourceAttribute = classDataSource,
                            InitializedAttributes = attributes
                        };

                        testBuilderContext.CopyStateBagTo(testSpecificContext);

                        var test = await BuildTestAsync(metadata, testData, testSpecificContext, cancellationToken: cancellationToken);
                        test.Context.SkipReason = skipReason;
                        tests.Add(test);
                    }
                }
            }

            // Transfer captured build-time output to all test contexts
            var capturedOutput = buildContext.GetCapturedOutput();
            var capturedErrorOutput = buildContext.GetCapturedErrorOutput();

            if (!string.IsNullOrEmpty(capturedOutput) || !string.IsNullOrEmpty(capturedErrorOutput))
            {
                // Also route ALL build-time output to TestSessionContext.
                // This intentionally covers all sharing types (None, PerClass, PerAssembly, etc.),
                // making TestSessionContext a superset of every data source's build-time output.
                // The primary motivation is SharedType.PerTestSession: a single Lazy<T>
                // (ExecutionAndPublication) means the factory runs exactly once in whichever
                // test's buildContext wins the parallel-build race, so the output would otherwise
                // be lost from every other test. Session-level routing removes that race.
                // Note: the winning test batch also receives the output via SetBuildTimeOutput
                // below, so its output appears in both TestContext and TestSessionContext.
                if (!string.IsNullOrEmpty(capturedOutput))
                    TestSessionContext.Current?.OutputWriter.Write(capturedOutput);
                if (!string.IsNullOrEmpty(capturedErrorOutput))
                    TestSessionContext.Current?.ErrorOutputWriter.Write(capturedErrorOutput);

                foreach (var test in tests)
                {
                    test.Context.SetBuildTimeOutput(capturedOutput, capturedErrorOutput);
                }
            }
        }
        catch (Exception ex)
        {
            // The build context was disposed on leaving the try block; stop routing output to it.
            TestBuildContext.Current = null;

            var failedTest = CreateFailedTestForDataGenerationError(metadata, ex);
            tests.Add(failedTest);
            return tests;
        }

        return tests;
    }

    private readonly struct BuildContextLease(TestBuildContext buildContext) : IDisposable
    {
        public void Dispose() => buildContext.DisposeWithoutClearingCurrent();
    }

    private static Type[] TryInferClassGenericsFromMethodData(TestMetadata metadata, object?[] methodData)
    {
        var genericClassType = metadata.TestClassType;
        var genericParameters = genericClassType.GetGenericArguments();
        var typeMapping = new Dictionary<Type, Type>();

        // Try to match method parameter types with actual data types
        var methodParameters = metadata.MethodMetadata.Parameters;
        for (var i = 0; i < Math.Min(methodParameters.Length, methodData.Length); i++)
        {
            var methodParam = methodParameters[i];
            var paramType = methodParam.Type;
            var argValue = methodData[i];

            if (argValue != null)
            {
                var argType = argValue.GetType();

                // If the parameter type is object (placeholder for generic parameter in source-gen)
                // and we have data, we can infer the type
                if (paramType == typeof(object))
                {
                    // Check if this corresponds to a class generic parameter
                    // by looking at the method metadata

                    if (methodParam.TypeInfo is GenericParameter { IsMethodParameter: false } gp)
                    {
                        var genericParamName = gp.Name;
                        // Find the matching generic parameter in the class
                        var matchingClassParam = genericParameters.FirstOrDefault(p => p.Name == genericParamName);
                        if (matchingClassParam != null)
                        {
                            typeMapping[matchingClassParam] = argType;
                        }
                    }
                }
            }
        }

        var resolvedTypes = new Type[genericParameters.Length];
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];
            if (!typeMapping.TryGetValue(genericParam, out var resolvedType))
            {
                throw new InvalidOperationException(
                    $"Could not resolve type for generic parameter '{genericParam.Name}' of type '{genericClassType.Name}' from method data");
            }
            resolvedTypes[i] = resolvedType;
        }

        return resolvedTypes;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type inference uses reflection on data sources and parameters")]
#endif
    private static Type[] TryInferClassGenericsFromDataSources(TestMetadata metadata)
    {
        var genericClassType = metadata.TestClassType;
        var genericParameters = genericClassType.GetGenericArguments();
        var typeMapping = new Dictionary<Type, Type>();

        // First, check if we have typed data sources that can help infer the generic type
        foreach (var dataSource in metadata.DataSources)
        {
            var dataSourceType = dataSource.GetType();

            // Check if this data source inherits from a generic base type
            var baseType = dataSourceType.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    var genericDef = baseType.GetGenericTypeDefinition();
                    var genericDefName = genericDef.FullName ?? genericDef.Name;

                    // Check if it's a typed data source attribute
                    if (genericDefName.Contains("DataSourceGeneratorAttribute`") ||
                        genericDefName.Contains("AsyncDataSourceGeneratorAttribute`"))
                    {
                        // Get the type argument (e.g., int from AsyncDataSourceGeneratorAttribute<int>)
                        var typeArgs = baseType.GetGenericArguments();
                        if (typeArgs.Length > 0 && genericParameters.Length > 0)
                        {
                            // For now, assume the first generic parameter maps to the data source type
                            // This handles simple cases like GenericClass<T> with IntDataSource
                            typeMapping[genericParameters[0]] = typeArgs[0];
                        }
                        break;
                    }
                }
                baseType = baseType.BaseType;
            }
        }

        if (HasInstanceDataAccessor(metadata.DataSources))
        {
            // Look at the test method parameters to find attributes that can help with generic type inference
            foreach (var param in metadata.MethodMetadata.Parameters)
            {
                // Get the actual parameter type from reflection
                var actualParamType = param.Type;

                // Check if the actual parameter type is a generic parameter of the class
                if (actualParamType.IsGenericParameter &&
                    genericParameters.Contains(actualParamType))
                {
                    // Check for Matrix attributes using reflection
                    var attrs = param.ReflectionInfo.GetCustomAttributes(false);

                    foreach (var attr in attrs)
                    {
                        var attrType = attr.GetType();

                        // Check if it's a generic Matrix attribute
                        if (attrType.IsGenericType &&
                            attrType.GetGenericTypeDefinition().Name.StartsWith("Matrix"))
                        {
                            var matrixTypeArg = attrType.GetGenericArguments()[0];
                            typeMapping[actualParamType] = matrixTypeArg;
                            break;
                        }
                    }
                }
            }
        }

        // Look for instance method data sources
        foreach (var dataSource in metadata.DataSources)
        {
            if (dataSource is InstanceMethodDataSourceAttribute instanceMethodDataSource)
            {
                // Get the method info
                var methodName = instanceMethodDataSource.MethodNameProvidingDataSource;
                var method = genericClassType.GetMethod(methodName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (method != null)
                {
                    var returnType = method.ReturnType;

                    // Check if return type is IEnumerable<T> or similar
                    if (returnType.IsGenericType)
                    {
                        var genericDef = returnType.GetGenericTypeDefinition();
                        if (genericDef == typeof(IEnumerable<>) ||
                            genericDef == typeof(IAsyncEnumerable<>) ||
                            genericDef == typeof(List<>) ||
                            genericDef == typeof(Task<>))
                        {
                            var elementType = returnType.GetGenericArguments()[0];

                            // If Task<T>, unwrap to get the actual type
                            if (genericDef == typeof(Task<>) && elementType.IsGenericType)
                            {
                                var innerGenericDef = elementType.GetGenericTypeDefinition();
                                if (innerGenericDef == typeof(IEnumerable<>) ||
                                    innerGenericDef == typeof(IAsyncEnumerable<>) ||
                                    innerGenericDef == typeof(List<>))
                                {
                                    elementType = elementType.GetGenericArguments()[0];
                                }
                            }

                            // Now try to match this element type with method parameters
                            // that use the class generic parameter
                            for (var i = 0; i < metadata.MethodMetadata.Parameters.Length; i++)
                            {
                                var paramType = metadata.MethodMetadata.Parameters[i].Type;
                                if (paramType == typeof(object)) // Placeholder for generic parameter
                                {
                                    var methodParam = metadata.MethodMetadata.Parameters[i];
                                    if (methodParam.TypeInfo is GenericParameter { IsMethodParameter: false } gp2)
                                    {
                                        var genericParamName = gp2.Name;
                                        var matchingClassParam = genericParameters.FirstOrDefault(p => p.Name == genericParamName);
                                        if (matchingClassParam != null)
                                        {
                                            typeMapping[matchingClassParam] = elementType;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        var resolvedTypes = new Type[genericParameters.Length];
        for (var i = 0; i < genericParameters.Length; i++)
        {
            var genericParam = genericParameters[i];
            if (!typeMapping.TryGetValue(genericParam, out var resolvedType))
            {
                throw new InvalidOperationException(
                    $"Could not resolve type for generic parameter '{genericParam.Name}' of type '{genericClassType.Name}' from data sources");
            }
            resolvedTypes[i] = resolvedType;
        }

        return resolvedTypes;
    }

    private static readonly IDataSourceAttribute[] _dataSourceArray = [NoDataSource.Instance];

    /// <summary>
    /// The object whose properties are injected for a data source: the wrapped source of a
    /// <see cref="GenericConstraintViolationDataSource"/>, whose rows it enumerates, else the source itself.
    /// </summary>
    private static object GetInjectionTarget(IDataSourceAttribute dataSource)
        => dataSource is GenericConstraintViolationDataSource { InnerDataSource: { } inner } ? inner : dataSource;

    private ValueTask<IDataSourceAttribute[]> GetDataSourcesAsync(IDataSourceAttribute[] dataSources, CancellationToken cancellationToken = default)
    {
        if (dataSources.Length == 0)
        {
            return new ValueTask<IDataSourceAttribute[]>(_dataSourceArray);
        }

        foreach (var dataSource in dataSources)
        {
            if (PropertyInjectionCache.HasInjectableProperties(SourceToInject(dataSource).GetType()))
            {
                return InjectDataSourcePropertiesAsync(dataSources, cancellationToken);
            }
        }

        // Nothing to inject — complete synchronously without an async state machine/Task.
        return new ValueTask<IDataSourceAttribute[]>(dataSources);
    }

    private async ValueTask<IDataSourceAttribute[]> InjectDataSourcePropertiesAsync(IDataSourceAttribute[] dataSources, CancellationToken cancellationToken)
    {
        // Inject properties into data sources during discovery (IAsyncInitializer deferred to execution)
        foreach (var dataSource in dataSources)
        {
            await _objectLifecycleService.InjectPropertiesAsync(SourceToInject(dataSource), cancellationToken: cancellationToken);
        }

        return dataSources;
    }

    /// <summary>
    /// A partition of a data source's rows holds plain values, but the source it was taken from joins
    /// the test's attributes (see <see cref="ScopeRowAttributesToTest"/>), so that source receives the
    /// property injection the test's other data sources do before its callbacks run.
    /// </summary>
    private static object SourceToInject(IDataSourceAttribute dataSource) =>
        dataSource is StaticDataSourceAttribute { PartitionOf: { } partitionedSource } ? partitionedSource : GetInjectionTarget(dataSource);

    /// <summary>
    /// Ensures a data source is initialized before use and returns data rows.
    /// This centralizes the initialization logic for all data source usage.
    /// </summary>
    private IAsyncEnumerable<Func<Task<object?[]?>>> GetInitializedDataRowsAsync(
        IDataSourceAttribute dataSource,
        TestMetadata metadata,
        DataGeneratorType generatorType,
        object? testClassInstance,
        object?[]? classInstanceArguments,
        TestBuilderContextAccessor contextAccessor,
        CancellationToken cancellationToken = default)
    {
        // NoDataSource always yields exactly one empty row and ignores its metadata — skip building
        // a DataGeneratorMetadata and an async-iterator state machine for every plain test.
        if (dataSource is NoDataSource)
        {
            return SingleEmptyRow.Instance;
        }

        var dataGeneratorMetadata = DataGeneratorMetadataCreator.CreateDataGeneratorMetadata(
            testMetadata: metadata,
            testSessionId: _sessionId,
            generatorType: generatorType,
            testClassInstance: testClassInstance,
            classInstanceArguments: classInstanceArguments,
            contextAccessor);

        // Fast path: nothing to inject (e.g. NoDataSource, [Arguments]) — enumerate the source directly.
        // Skips a wrapping async-iterator state machine and, more importantly, avoids materializing the
        // builder context's StateBag/Events (a ConcurrentDictionary per test) just to pass them along.
        if (!PropertyInjectionCache.HasInjectableProperties(GetInjectionTarget(dataSource).GetType()))
        {
            return dataSource.GetDataRowsAsync(dataGeneratorMetadata);
        }

        return GetInitializedDataRowsWithInjectionAsync(dataSource, dataGeneratorMetadata, cancellationToken);
    }

    private async IAsyncEnumerable<Func<Task<object?[]?>>> GetInitializedDataRowsWithInjectionAsync(
        IDataSourceAttribute dataSource,
        DataGeneratorMetadata dataGeneratorMetadata,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Inject properties into data source during discovery (IAsyncInitializer deferred to execution)
        await _objectLifecycleService.InjectPropertiesAsync(
            GetInjectionTarget(dataSource),
            dataGeneratorMetadata.TestBuilderContext.Current.StateBag,
            dataGeneratorMetadata.TestInformation,
            dataGeneratorMetadata.TestBuilderContext.Current.Events,
            cancellationToken);

        // Now get data rows from the property-injected data source (properties are injected in place)
        await foreach (var dataRow in dataSource.GetDataRowsAsync(dataGeneratorMetadata))
        {
            yield return dataRow;
        }
    }

    /// <summary>
    /// The rows of <see cref="NoDataSource"/>: a single row with no arguments. Stateless, so one
    /// instance is shared; each enumeration only allocates a tiny enumerator.
    /// </summary>
    private sealed class SingleEmptyRow : IAsyncEnumerable<Func<Task<object?[]?>>>
    {
        public static readonly SingleEmptyRow Instance = new();

        private static readonly Task<object?[]?> EmptyRowTask = Task.FromResult<object?[]?>([]);
        private static readonly Func<Task<object?[]?>> EmptyRowFactory = static () => EmptyRowTask;

        public IAsyncEnumerator<Func<Task<object?[]?>>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator();

        private sealed class Enumerator : IAsyncEnumerator<Func<Task<object?[]?>>>
        {
            private bool _moved;

            public Func<Task<object?[]?>> Current => EmptyRowFactory;

            public ValueTask<bool> MoveNextAsync()
            {
                if (_moved)
                {
                    return new ValueTask<bool>(false);
                }

                _moved = true;
                return new ValueTask<bool>(true);
            }

            public ValueTask DisposeAsync() => default;
        }
    }

    /// <summary>
    /// Fetches a class row before its method data sources are evaluated, against a context of its own,
    /// so the events the data source subscribes while it creates the row's objects can travel with them
    /// to the test that takes them (#7205). The context shares the state bag of the context the fetch used
    /// to run against, which is current again once the fetch returns.
    /// </summary>
    private static async ValueTask<(object?[] Data, TestDataRowMetadata? Metadata, TestContextEvents? Events)> FetchFirstClassRowAsync(
        Func<Task<object?[]?>> classDataFactory,
        IDataSourceAttribute classDataSource,
        TestMetadata metadata,
        TestBuilderContextAccessor contextAccessor)
    {
        if (classDataSource is NoDataSource)
        {
            // Yields one empty row and subscribes nothing.
            var (emptyRow, emptyRowMetadata) = DataUnwrapper.UnwrapWithMetadata(await classDataFactory() ?? []);
            return (emptyRow, emptyRowMetadata, null);
        }

        var previousContext = contextAccessor.Current;
        var fetchContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata,
            ClassDataSourceAttribute = classDataSource,
            InitializedAttributes = previousContext.InitializedAttributes,
            ClassConstructor = previousContext.ClassConstructor
        };
        fetchContext.ShareStateBagWith(previousContext);

        contextAccessor.Current = fetchContext;
        try
        {
            var (data, rowMetadata) = DataUnwrapper.UnwrapWithMetadata(await classDataFactory() ?? []);
            return (data, rowMetadata, fetchContext.EventsIfCreated);
        }
        finally
        {
            contextAccessor.Current = previousContext;
        }
    }

    /// <summary>
    /// The objects of a class data row that were fetched but that no test holds yet. The builder fetches a
    /// class row once before it evaluates the method data sources, and its tests used to fetch it again,
    /// so a non-shared source created an extra instance per class row that nothing tracked or disposed.
    /// The next test built from the row takes the unclaimed objects instead of calling the factory (#7157).
    /// </summary>
    /// <remarks>
    /// When the row needs a temporary instance for an instance method data source, that instance is built
    /// from these objects and disposed once the method rows are read, so no test may receive them. They
    /// are lent to the instance instead, and the next test built from the row tracks them without
    /// receiving them, so they are still disposed.
    /// <para>
    /// The events that the data source subscribed on the builder context while the row was first fetched
    /// (such as the disposal of a dependency injection scope) travel with these objects, so the test
    /// that takes or tracks them also raises those events (#7205).
    /// </para>
    /// </remarks>
    private sealed class UnclaimedClassRow(object?[] data, TestDataRowMetadata? metadata, TestContextEvents? events)
    {
        private object?[]? _data = data;
        private TestDataRowMetadata? _metadata = metadata;
        private object?[]? _lent;
        private TestContextEvents? _events = events;

        /// <summary>
        /// Takes the unclaimed objects, or fetches a new row from <paramref name="classDataFactory"/> when a
        /// test already holds them. Any events of the unclaimed or lent objects move to
        /// <paramref name="builderContext"/> first, the context the row is fetched against, so a new fetch
        /// adds its own subscriptions to the same container.
        /// </summary>
        public ValueTask<(object?[] Data, TestDataRowMetadata? Metadata)> TakeOrFetchAsync(
            Func<Task<object?[]?>> classDataFactory,
            TestBuilderContext builderContext)
        {
            if (TakeEvents() is { } events)
            {
                // The context is fresh, so assigning cannot drop a subscription made before the take.
                System.Diagnostics.Debug.Assert(builderContext.EventsIfCreated is null, "Row events would replace existing subscriptions.");
                builderContext.Events = events;
            }

            if (_data is { } data)
            {
                var row = (data, _metadata);
                Clear();
                return new ValueTask<(object?[] Data, TestDataRowMetadata? Metadata)>(row);
            }

            return FetchAsync(classDataFactory);

            static async ValueTask<(object?[] Data, TestDataRowMetadata? Metadata)> FetchAsync(Func<Task<object?[]?>> factory)
                => DataUnwrapper.UnwrapWithMetadata(await factory() ?? []);
        }

        /// <summary>
        /// Hands back objects, and the events of the context they were taken against, that no test ended up
        /// holding, so the next test takes them.
        /// </summary>
        public void Return(object?[] data, TestDataRowMetadata? metadata, TestContextEvents? events)
        {
            _data = data;
            _metadata = metadata;
            _events = events;
        }

        /// <summary>
        /// Takes the events subscribed while the unclaimed or lent objects were fetched, for the test that
        /// holds or tracks those objects.
        /// </summary>
        public TestContextEvents? TakeEvents()
        {
            var events = _events;
            _events = null;
            return events;
        }

        /// <summary>
        /// Marks the unclaimed objects as held by a test that did not take them through <see cref="TakeOrFetchAsync"/>.
        /// </summary>
        public void Clear()
        {
            _data = null;
            _metadata = null;
            _lent = null;
            _events = null;
        }

        /// <summary>
        /// Marks the unclaimed objects as used by a temporary instance, so no test receives them, but the
        /// next test tracks them through <see cref="TakeLentObjects"/> or <see cref="WithLentObjects"/>.
        /// </summary>
        public void LendToTemporaryInstance()
        {
            _lent = _data;
            _data = null;
            _metadata = null;
        }

        /// <summary>
        /// Takes the objects lent to a temporary instance, for a test to track without receiving them.
        /// </summary>
        public object?[] TakeLentObjects()
        {
            var lent = _lent ?? [];
            _lent = null;
            return lent;
        }

        /// <summary>
        /// Adds the objects lent to a temporary instance to a failed test's class row objects, which the
        /// failed test tracks without receiving.
        /// </summary>
        public object?[]? WithLentObjects(object?[]? classData)
        {
            var lent = TakeLentObjects();
            if (lent.Length == 0)
            {
                return classData;
            }

            if (classData is not { Length: > 0 })
            {
                return lent;
            }

            var combined = new object?[lent.Length + classData.Length];
            lent.CopyTo(combined, 0);
            classData.CopyTo(combined, lent.Length);
            return combined;
        }
    }

    private static async ValueTask DisposeInstanceForMethodDataSourcesAsync(object? instance)
    {
        if (instance is null or PlaceholderInstance or SkippedTestInstance)
        {
            return;
        }

        try
        {
            switch (instance)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
        catch
        {
            // Match normal test instance disposal: disposal failures do not fail the test.
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hook discovery service handles mode-specific logic; reflection calls suppressed in AOT mode")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Hook discovery service handles mode-specific logic; dynamic code suppressed in AOT mode")]
    public async ValueTask<AbstractExecutableTest> BuildTestAsync(TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext, CancellationToken cancellationToken = default)
    {
        // Discover instance hooks for closed generic types (no-op in source gen mode)
        if (metadata.TestClassType is { IsGenericType: true, IsGenericTypeDefinition: false })
        {
            _hookDiscoveryService.DiscoverInstanceHooksForType(metadata.TestClassType);
        }

        var testId = TestIdentifierService.GenerateTestId(metadata, testData);

        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext, cancellationToken);

        context.Metadata.TestDetails.ClassInstance = PlaceholderInstance.Instance;

        // Apply metadata from TestDataRow or data source attributes
        if (testData.Metadata is { } dataRowMetadata)
        {
            // Apply custom display name (will be processed by DisplayNameBuilder)
            if (!string.IsNullOrEmpty(dataRowMetadata.DisplayName))
            {
                context.SetDataSourceDisplayName(dataRowMetadata.DisplayName!);
            }

            // Apply data expression for default display name formatting (only when no explicit DisplayName)
            if (string.IsNullOrEmpty(dataRowMetadata.DisplayName) && !string.IsNullOrEmpty(dataRowMetadata.DataExpression))
            {
                context.SetDataSourceExpression(dataRowMetadata.DataExpression!);
            }

            // Apply skip reason from data source
            if (!string.IsNullOrEmpty(dataRowMetadata.Skip))
            {
                context.SkipReason = dataRowMetadata.Skip;
            }

            // Apply categories from data source
            if (dataRowMetadata.Categories is { Length: > 0 })
            {
                foreach (var category in dataRowMetadata.Categories)
                {
                    context.Metadata.TestDetails.Categories.Add(category);
                }
            }
        }

        // Arguments will be tracked by TestArgumentTrackingService during TestRegistered event
        // This ensures proper reference counting for shared instances

        // Create the test object BEFORE invoking event receivers
        // This ensures context.InternalExecutableTest is set for error handling in registration
        var creationContext = new ExecutableTestCreationContext
        {
            TestId = testId,
            DisplayName = context.GetDisplayName(),
            Arguments = testData.MethodData,
            ClassArguments = testData.ClassData,
            Context = context,
            TestClassInstanceFactory = testData.TestClassInstanceFactory,
            ResolvedMethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ResolvedClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        var test = metadata.CreateExecutableTestFactory(creationContext, metadata);

        // Set InternalExecutableTest so it's available during registration for error handling
        context.InternalExecutableTest = test;

        // Test argument registration (property injection + reference counting) is NOT done here.
        // It happens post-filter in TestFilterService.RegisterTest so that shared-object reference
        // counts only include tests that will actually execute. Registering at build time inflated
        // the counts with built-but-filtered-out tests (e.g. [Explicit] siblings or single
        // [Arguments] cases selected by an IDE uid filter), so the count never drained to zero and
        // shared fixtures were never disposed (#6151).
        // ITestRegisteredEventReceiver and ITestDiscoveryEventReceiver are invoked later in
        // InvokePostResolutionEventsAsync after dependencies are resolved.

        return test;
    }

    /// <summary>
    /// Invokes event receivers after dependencies have been resolved.
    /// This is called from TestDiscoveryService after all tests are built and dependencies resolved.
    /// </summary>
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public ValueTask InvokePostResolutionEventsAsync(AbstractExecutableTest test)
    {
        var context = test.Context;

        // Populate TestContext._dependencies from resolved test.Dependencies
        // This makes dependencies available to event receivers and hooks
        PopulateDependenciesOnly(test);

        // Invoke discovery event receivers (for all tests during discovery phase)
        // Note: ITestRegisteredEventReceiver.OnTestRegistered is NOT called here.
        // Registration events fire later in TestFilterService.RegisterTest for filtered tests only.
        // This ensures OnTestRegistered is called exactly once per test that will actually run.
        var receivers = context.GetTestDiscoveryReceivers();
        if (receivers.Length == 0)
        {
            // Nothing observes the context — skip the AsyncLocal write (a new ExecutionContext and
            // value map per test), the DiscoveredTestContext and the async state machine.
            return default;
        }

        return InvokeDiscoveryEventReceiversAsync(context, receivers);
    }

    private static async ValueTask InvokeDiscoveryEventReceiversAsync(TestContext context, ITestDiscoveryEventReceiver[] receivers)
    {
        // Set TestContext.Current so output capture works via AsyncLocal
        // This ensures any console output during event receiver invocation is captured
        TestContext.Current = context;

        var discoveredContext = new DiscoveredTestContext(
            context.Metadata.TestDetails.TestName,
            context);

        foreach (var receiver in receivers)
        {
            await receiver.OnTestDiscovered(discoveredContext);
        }
    }

    /// <inheritdoc />
    public void PopulateDependenciesOnly(AbstractExecutableTest test)
    {
        var context = test.Context;

        // Skip if already populated to make this idempotent
        if (context._dependenciesPopulated)
        {
            return;
        }

        PopulateDependencies(test, context);
        context._dependenciesPopulated = true;
    }

    private static void PopulateDependencies(AbstractExecutableTest test, TestContext context)
    {
        if (test.Dependencies.Length == 0)
        {
            return;
        }

        var dependencies = context._dependencies ??= [];

        var collected = new HashSet<TestDetails>();
        var visited = new HashSet<AbstractExecutableTest>();
        CollectAllDependencies(test, collected, visited);

        foreach (var dependency in collected)
        {
            dependencies.Add(dependency);
        }
    }

    private static void CollectAllDependencies(AbstractExecutableTest test, HashSet<TestDetails> collected, HashSet<AbstractExecutableTest> visited)
    {
        if (!visited.Add(test))
        {
            return;
        }

        foreach (var dependency in test.Dependencies)
        {
            if (collected.Add(dependency.Test.Context.Metadata.TestDetails))
            {
                CollectAllDependencies(dependency.Test, collected, visited);
            }
        }
    }

    /// <summary>
    /// Checks if a test has basic SkipAttribute instances that can be evaluated at discovery time.
    /// Returns null if no skip attributes, a skip reason if basic skip attributes are found,
    /// or empty string if custom skip attributes requiring runtime evaluation are found.
    /// </summary>
    private static string? GetBasicSkipReason(TestMetadata metadata, Attribute[]? cachedAttributes = null)
    {
        var attributes = cachedAttributes ?? metadata.GetOrCreateAttributes();

        SkipAttribute? firstSkipAttribute = null;

        // Check if all skip attributes are basic (non-derived) SkipAttribute instances
        foreach (var attribute in attributes)
        {
            if (attribute is not SkipAttribute skipAttribute)
            {
                continue;
            }

            firstSkipAttribute ??= skipAttribute;

            var attributeType = skipAttribute.GetType();
            if (attributeType != typeof(SkipAttribute))
            {
                // This is a derived skip attribute that might have custom ShouldSkip logic
                return string.Empty; // Indicates custom skip attributes that need runtime evaluation
            }
        }

        // All skip attributes are basic SkipAttribute instances
        // Return the first reason (they all should skip), or null if no skip attributes.
        // An empty reason is normalized, because empty means "custom skip attribute" here (#7077).
        return firstSkipAttribute is null ? null : SkipAttribute.NormalizeReason(firstSkipAttribute.Reason);
    }

    private async ValueTask<TestContext> CreateTestContextAsync(string testId, TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext, CancellationToken cancellationToken = default)
    {
        // Use attributes from context if available, or create new ones
        var attributes = testBuilderContext.InitializedAttributes ?? await InitializeAttributesAsync(metadata.GetOrCreateAttributes(), cancellationToken);

        attributes = ScopeRowAttributesToTest(attributes, testBuilderContext.ClassDataSourceAttribute, testBuilderContext.DataSourceAttribute);

        var testDetails = new TestDetails(attributes)
        {
            TestId = testId,
            TestName = GetTestName(metadata, testData),
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = testData.MethodData,
            TestClassArguments = testData.ClassData,
            UnreceivedDataRowObjects = testData.UnreceivedClassRowObjects,
            TestFilePath = metadata.FilePath,
            TestLineNumber = metadata.LineNumber,
            TestStartColumnNumber = metadata.StartColumnNumber,
            TestEndLineNumber = metadata.EndLineNumber,
            TestEndColumnNumber = metadata.EndColumnNumber,
            ReturnType = metadata.MethodMetadata.ReturnType ?? typeof(void),
            MethodMetadata = metadata.MethodMetadata,
            // Built on first lookup; most tests only ever ask ContainsKey (e.g. [Explicit]).
            AttributesByType = attributes.Length == 0 ? AttributeDictionaryHelper.Empty : new LazyAttributeDictionary(attributes),
            MethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestClassType,
            testBuilderContext,
            testDetails,
            CancellationToken.None);

        return context;
    }

    private static bool HasDeferredDataSource(TestMetadata metadata)
    {
        foreach (var dataSource in metadata.DataSources)
        {
            if (dataSource.DeferEnumeration)
            {
                return true;
            }
        }

        foreach (var dataSource in metadata.ClassDataSources)
        {
            if (dataSource.DeferEnumeration)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the single placeholder node for a test whose data source enumeration is deferred.
    /// The placeholder carries no class/method data and is never executed as a real test — it exists
    /// so discovery shows one node, and DeferredTestExpander uses its <see cref="TestMetadata"/> to build
    /// the real cases at runtime.
    /// </summary>
    private async Task<AbstractExecutableTest> BuildDeferredPlaceholderAsync(
        TestMetadata metadata,
        TestBuilderContext testBuilderContext,
        CancellationToken cancellationToken)
    {
        var testId = TestIdentifierService.GenerateDeferredPlaceholderTestId(metadata);

        var testData = new TestData
        {
            TestClassInstanceFactory = static () => Task.FromResult<object>(PlaceholderInstance.Instance),
            ClassDataSourceAttributeIndex = 0,
            ClassDataLoopIndex = 0,
            ClassData = [],
            MethodDataSourceAttributeIndex = 0,
            MethodDataLoopIndex = 0,
            MethodData = [],
            RepeatIndex = 0,
            InheritanceDepth = metadata.InheritanceDepth,
            ResolvedClassGenericArguments = Type.EmptyTypes,
            ResolvedMethodGenericArguments = Type.EmptyTypes
        };

        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext, cancellationToken);

        return new DeferredEnumerationExecutableTest
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context
        };
    }

    private AbstractExecutableTest CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        return CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination());
    }

    /// <summary>
    /// Creates a failed test for one data row. The row's indexes go into the test id, so several failing
    /// rows of the same method (or repeats of one row) keep distinct ids, as passing rows do.
    /// </summary>
    /// <param name="classData">The class row's objects, if the row produced them before it failed.</param>
    /// <param name="methodData">The method row's objects, if the row produced them before it failed.</param>
    /// <param name="rowEvents">The events the row's data sources subscribed while it was fetched, if any.</param>
    private AbstractExecutableTest CreateFailedTestForDataRowError(
        TestMetadata metadata,
        Exception exception,
        int classDataAttributeIndex,
        int classDataLoopIndex,
        int methodDataAttributeIndex,
        int methodDataLoopIndex,
        int repeatIndex,
        object?[]? classData,
        object?[]? methodData,
        TestContextEvents? rowEvents)
    {
        return CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination
        {
            ClassDataSourceIndex = classDataAttributeIndex,
            ClassLoopIndex = classDataLoopIndex,
            MethodDataSourceIndex = methodDataAttributeIndex,
            MethodLoopIndex = methodDataLoopIndex,
            RepeatIndex = repeatIndex
        }, classData, methodData, rowEvents);
    }

    /// <param name="metadata">The test's metadata.</param>
    /// <param name="exception">The exception to report.</param>
    /// <param name="combination">The row's indexes, which go into the test id.</param>
    /// <param name="classData">The class row's objects, if the row produced them before it failed.</param>
    /// <param name="methodData">The method row's objects, if the row produced them before it failed.</param>
    /// <param name="rowEvents">The events the row's data sources subscribed while it was fetched, if any.</param>
    /// <remarks>
    /// The failed test never receives the row's objects, but it carries them in
    /// <see cref="TestDetails.UnreceivedDataRowObjects"/>, so the object tracker counts them when the test is
    /// registered for execution and releases them when it completes. Releasing a reference disposes an
    /// object only when no other test still holds it, so shared and keyed instances stay alive (#7123).
    /// It also takes the row's events, so it raises their <c>OnDispose</c> handlers (a dependency
    /// injection scope's disposal, for example) as a test built from the row would (#7326).
    /// </remarks>
    private AbstractExecutableTest CreateFailedTestForDataGenerationError(
        TestMetadata metadata,
        Exception exception,
        TestDataCombination combination,
        object?[]? classData = null,
        object?[]? methodData = null,
        TestContextEvents? rowEvents = null)
    {
        exception = ToReportedException(exception);
        var testId = TestIdentifierService.GenerateFailedTestId(metadata, combination);

        var testDetails = CreateFailedTestDetails(metadata, testId, CombineRowObjects(classData, methodData));
        var context = CreateFailedTestContext(metadata, testDetails, rowEvents);
        var now = DateTimeOffset.UtcNow;

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context,
            State = TestState.Failed,
            Result = new TestResult
            {
                State = TestState.Failed,
                Start = now,
                End = now,
                Duration = TimeSpan.Zero,
                Exception = exception,
                ComputerName = EnvironmentHelper.MachineName,
                TestContext = context
            }
        };
    }

    /// <summary>
    /// Reflection mode discovers a generic method once, as its open definition, and resolves its
    /// type arguments per data row. Append them here so the test name matches source-gen mode,
    /// which emits one entry per instantiation with the type arguments already in its name (#7105).
    /// </summary>
    private static string GetTestName(TestMetadata metadata, TestData testData)
    {
        var resolvedMethodGenericArguments = testData.ResolvedMethodGenericArguments;

        if (resolvedMethodGenericArguments.Length == 0
            // Closed by MakeGenericMethod, so the metadata name already has its type arguments.
            || metadata.GenericMethodTypeArguments is { Length: > 0 }
            // A custom name (such as a [DisplayName] template) is not decorated.
            || !string.Equals(metadata.TestName, metadata.TestMethodName, StringComparison.Ordinal))
        {
            return metadata.TestName;
        }

        return TypeNameFormatter.AppendGenericArguments(metadata.TestName, resolvedMethodGenericArguments);
    }

    private static object?[] CombineRowObjects(object?[]? classData, object?[]? methodData)
    {
        if (classData is not { Length: > 0 })
        {
            return methodData ?? [];
        }

        if (methodData is not { Length: > 0 })
        {
            return classData;
        }

        return [.. classData, .. methodData];
    }

    /// <summary>
    /// A <see cref="GenericConstraintViolationDataSource"/> row raises a
    /// <see cref="GenericConstraintViolationRowException"/> that carries the wrapped row's values. Returns
    /// the exception to report, a plain <see cref="GenericTypeResolutionException"/> as in reflection mode,
    /// and the row's unwrapped objects, so the failed test can track them for disposal (#7123).
    /// </summary>
    /// <remarks>
    /// Never throws: callers run it inside a row's catch block, so an exception here would abandon the
    /// method's remaining rows.
    /// </remarks>
    internal static (Exception ReportedException, object?[]? RowData) UnpackConstraintViolation(Exception exception, ParameterMetadata[] parameters)
    {
        if (exception is not GenericConstraintViolationRowException rowException)
        {
            return (exception, null);
        }

        object?[]? rowData = null;
        if (rowException.Row is { } row)
        {
            try
            {
                rowData = DataUnwrapper.UnwrapWithTypes(row, parameters);
            }
            catch (Exception)
            {
                // Unwrapping invokes the inner Func of a TestDataRow<Func<T>>, which can throw. The row
                // already fails with the constraint violation, so track what the row holds without
                // invoking anything. A TestDataRow wrapper is not itself disposable, so track its value
                // instead; a Func that threw created no value, so there is nothing to track for it.
                rowData = row is [ITestDataRow testDataRow]
                    ? testDataRow.GetData() is { } data and not Delegate ? [data] : []
                    : row;
            }
        }

        return (ToReportedException(rowException), rowData);
    }

    /// <summary>
    /// Replaces the internal <see cref="GenericConstraintViolationRowException"/> with the plain
    /// <see cref="GenericTypeResolutionException"/> that reflection mode reports. Every failed-test factory
    /// calls this, so a catch site that does not use <see cref="UnpackConstraintViolation"/> cannot leak
    /// the internal type to users.
    /// </summary>
    internal static Exception ToReportedException(Exception exception)
        => exception is GenericConstraintViolationRowException rowException
            ? new GenericTypeResolutionException(rowException.Message)
            : exception;

    private TestDetails CreateFailedTestDetails(TestMetadata metadata, string testId, object?[] failedDataRowObjects)
    {
        return new TestDetails([])
        {
            TestId = testId,
            UnreceivedDataRowObjects = failedDataRowObjects,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null!,
            TestMethodArguments = [],
            TestClassArguments = [],
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber,
            TestStartColumnNumber = metadata.StartColumnNumber,
            TestEndLineNumber = metadata.EndLineNumber,
            TestEndColumnNumber = metadata.EndColumnNumber,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            AttributesByType = AttributeDictionaryHelper.Empty
        };
    }

    /// <summary>
    /// Gives a test the data sources that produced it. A data source that is also an
    /// <see cref="ITestRegisteredEventReceiver"/>, such as <see cref="ArgumentsAttribute"/>, carries
    /// per-row settings (Skip, DisplayName, Categories), so only the producing row may stay in the
    /// test's attributes: a class's attributes list every class-level row, and listing them all
    /// would apply one row's settings to its siblings (#7106). The producing class and method data
    /// sources are appended instead.
    /// </summary>
    internal static Attribute[] ScopeRowAttributesToTest(Attribute[] attributes, IDataSourceAttribute? classDataSource, IDataSourceAttribute? methodDataSource)
    {
        // Reflection discovery splits a class data source whose rows infer different generic type
        // arguments into one partition per instantiation. The partition stands in for the original
        // source's data only, so the original keeps its place here and its registration callback.
        if (classDataSource is StaticDataSourceAttribute { PartitionOf: { } partitionedSource })
        {
            classDataSource = partitionedSource;
        }

        var appendClass = IsRowScopedDataSource(classDataSource);
        var appendMethod = methodDataSource is not null and not NoDataSource;

        var rowAttributeCount = 0;
        foreach (var attribute in attributes)
        {
            if (IsRowScopedDataSource(attribute))
            {
                rowAttributeCount++;
            }
        }

        if (rowAttributeCount == 0 && !appendClass && !appendMethod)
        {
            return attributes;
        }

        var result = new Attribute[attributes.Length - rowAttributeCount + (appendClass ? 1 : 0) + (appendMethod ? 1 : 0)];
        var index = 0;
        foreach (var attribute in attributes)
        {
            if (!IsRowScopedDataSource(attribute))
            {
                result[index++] = attribute;
            }
        }

        if (appendClass)
        {
            result[index++] = (Attribute)classDataSource!;
        }

        if (appendMethod)
        {
            result[index] = (Attribute)methodDataSource!;
        }

        return result;
    }

    private static bool IsRowScopedDataSource(object? attribute) =>
        attribute is IDataSourceAttribute and ITestRegisteredEventReceiver and Attribute;

    private ValueTask<Attribute[]> InitializeAttributesAsync(Attribute[] attributes, CancellationToken cancellationToken = default)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is IDataSourceAttribute && PropertyInjectionCache.HasInjectableProperties(attribute.GetType()))
            {
                return InitializeAttributesCoreAsync(attributes, cancellationToken);
            }
        }

        // No data source attribute needs property injection — complete synchronously.
        return new ValueTask<Attribute[]>(attributes);
    }

    private async ValueTask<Attribute[]> InitializeAttributesCoreAsync(Attribute[] attributes, CancellationToken cancellationToken)
    {
        // Inject properties into data source attributes during discovery
        // IAsyncInitializer.InitializeAsync is deferred to execution time
        foreach (var attribute in attributes)
        {
            if (attribute is IDataSourceAttribute dataSource)
            {
                await _objectLifecycleService.InjectPropertiesAsync(dataSource, cancellationToken: cancellationToken);
            }
        }

        return attributes;
    }

    private TestContext CreateFailedTestContext(TestMetadata metadata, TestDetails testDetails, TestContextEvents? rowEvents)
    {
        var builderContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata
        };

        if (rowEvents is not null)
        {
            builderContext.Events = rowEvents;
        }

        var context = _contextProvider.CreateTestContext(
            metadata.TestClassType,
            builderContext,
            testDetails,
            CancellationToken.None);

        return context;
    }



    private AbstractExecutableTest CreateFailedTestForInstanceDataSourceError(TestMetadata metadata, Exception exception, object?[] classData, TestContextEvents? classRowEvents)
    {
        return CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination(), classData, rowEvents: classRowEvents);
    }

    private AbstractExecutableTest CreateFailedTestForClassDataSourceCircularDependency(TestMetadata metadata)
    {
        var instanceClassDataSources = metadata.ClassDataSources
            .Where(ds => ds is IAccessesInstanceData)
            .Select(ds => ds.GetType().Name);

        var dataSourceNames = string.Join(", ", instanceClassDataSources);
        var genericParams = string.Join(", ", metadata.TestClassType.GetGenericArguments().Select(t => t.Name));

        var message = $"Cannot use instance method data sources ({dataSourceNames}) for class constructor arguments with generic test class '{metadata.TestClassType.Name}<{genericParams}>'. " +
                      "This creates a circular dependency: instance data sources need an instance, but the instance needs constructor arguments from class data sources. " +
                      "Consider using static method data sources for class constructor arguments instead.";

        var exception = new InvalidOperationException(message);
        return CreateFailedTestForDataGenerationError(metadata, exception);
    }

    private static bool IsDataCompatibleWithExpectedTypes(TestMetadata metadata, object?[] methodData)
    {
        // Get the expected generic types - check both method and class type arguments
        var expectedTypes = metadata.GenericMethodTypeArguments;

        // For concrete instantiations of generic classes, check the class type arguments
        if ((expectedTypes == null || expectedTypes.Length == 0) && metadata.TestClassType.IsConstructedGenericType)
        {
            expectedTypes = metadata.TestClassType.GetGenericArguments();
        }

        if (expectedTypes == null || expectedTypes.Length == 0)
        {
            return true; // No specific types expected, allow all data
        }

        // Check if any method parameter actually uses the method's generic type parameters.
        // If none of the parameters use T, then data compatibility with the generic type doesn't matter.
        // This is important for methods like GenericMethod<T>(string input) where the parameter
        // is a concrete type (string) and doesn't depend on the generic type T.
        if (metadata.GenericMethodTypeArguments is { Length: > 0 })
        {
            var anyParameterUsesMethodGeneric = false;
            foreach (var param in metadata.MethodMetadata.Parameters)
            {
                if (ParameterUsesMethodGenericType(param.TypeInfo))
                {
                    anyParameterUsesMethodGeneric = true;
                    break;
                }
            }

            if (!anyParameterUsesMethodGeneric)
            {
                // None of the method parameters use the method's generic type parameters.
                // The data doesn't need to match the generic types - allow all data.
                return true;
            }
        }

        // For generic methods, we need to check if the data types match the expected types
        // The key is to determine what type of data this data source produces

        // Look for any non-null data in the methodData array to determine the actual types
        Type? actualDataType = null;
        object? sampleData = null;

        foreach (var data in methodData)
        {
            if (data != null)
            {
                sampleData = data;
                actualDataType = data.GetType();
                break;
            }
        }

        if (actualDataType == null || sampleData == null)
        {
            return true; // Can't determine type, allow it
        }

        // For AggregateBy test, the first generic type parameter is TSource
        if (expectedTypes.Length > 0)
        {
            var expectedElementType = expectedTypes[0];

            // For simple value types from Arguments attributes, check direct type compatibility
            if (methodData.Length == 1 && sampleData != null)
            {
                // Direct type check for the argument value
                return actualDataType == expectedElementType;
            }

            // Check various data source types
            if (actualDataType.Name == "RangeIterator")
            {
                // RangeIterator produces integers
                return expectedElementType == typeof(int);
            }

            // For compiler-generated array types like <>z__ReadOnlyArray`1
            if (actualDataType.Name.Contains("ReadOnlyArray") || actualDataType.Name.Contains("__Array"))
            {
                // These compiler-generated arrays implement IEnumerable but are not System.Array
                // We need to check the generic type argument instead
                if (actualDataType.IsGenericType)
                {
                    var genericArgs = actualDataType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        var elementType = genericArgs[0];
                        return elementType == expectedElementType;
                    }
                }

                // If we can't determine the generic type, reject it
                return false;
            }

            // Check regular arrays
            if (actualDataType.IsArray)
            {
                var elementType = actualDataType.GetElementType();
                if (elementType != null)
                {
                    return elementType == expectedElementType;
                }

                // For arrays where we can't get element type, check actual content
                if (sampleData is Array { Length: > 0 } arr)
                {
                    var firstElement = arr.GetValue(0);
                    if (firstElement != null)
                    {
                        return firstElement.GetType() == expectedElementType;
                    }
                }
            }

            // For other enumerable types, check if the actual data type matches expected
            if (actualDataType.IsGenericType)
            {
                var genericArgs = actualDataType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    return genericArgs[0] == expectedElementType;
                }
            }
        }

        // Default to false - if we can't determine compatibility, reject it
        return false;
    }

    /// <summary>
    /// Checks if a parameter's type involves a method generic type parameter.
    /// Returns true for parameters like T, List&lt;T&gt;, etc. where T is a method generic parameter.
    /// Returns false for concrete types like string, int, etc.
    /// </summary>
    private static bool ParameterUsesMethodGenericType(TypeInfo? typeInfo)
    {
        return typeInfo switch
        {
            GenericParameter { IsMethodParameter: true } => true,
            ConstructedGeneric cg => cg.TypeArguments.Any(ParameterUsesMethodGenericType),
            _ => false
        };
    }

    private static Type? GetExpectedTypeForParameter(ParameterMetadata param, Type[] genericTypeArgs)
    {
        // If it's a direct generic parameter (e.g., T)
        if (param.TypeInfo is GenericParameter gp)
        {
            var position = gp.Position;
            if (position < genericTypeArgs.Length)
            {
                return genericTypeArgs[position];
            }
        }

        // For constructed generic types, check first type argument
        if (param.TypeInfo is ConstructedGeneric cg && cg.TypeArguments.Length > 0)
        {
            if (cg.TypeArguments[0] is GenericParameter firstGp)
            {
                var position = firstGp.Position;
                if (position < genericTypeArgs.Length)
                {
                    // Return the element type - we'll check compatibility in IsTypeCompatible
                    return genericTypeArgs[position];
                }
            }
        }

        return null;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Type compatibility checking uses reflection")]
#endif
    private static bool IsTypeCompatible(Type actualType, Type expectedType)
    {
        // Direct match
        if (actualType == expectedType)
        {
            return true;
        }

        // For the data source filtering, we're mainly concerned with checking if the
        // data types match the expected generic type parameters.
        // For IEnumerable<T>, we need to check if the actual data is IEnumerable<int> vs IEnumerable<string> etc.

        // If we're expecting a specific element type (from generic parameter resolution),
        // check if the actual type is an enumerable of that element type
        if (actualType.IsGenericType)
        {
            var actualGenericDef = actualType.GetGenericTypeDefinition();

            // Check common collection types
            if (actualGenericDef == typeof(IEnumerable<>) ||
                actualGenericDef == typeof(List<>) ||
                actualGenericDef == typeof(IList<>) ||
                actualGenericDef == typeof(ICollection<>) ||
                actualGenericDef == typeof(HashSet<>) ||
                actualGenericDef == typeof(ISet<>))
            {
                var actualElementType = actualType.GetGenericArguments()[0];
                // Check if the element types match
                return actualElementType == expectedType;
            }

            // For arrays that come from Range operations
            if (actualType.IsArray)
            {
                var actualElementType = actualType.GetElementType();
                return actualElementType == expectedType;
            }
        }

        // For non-generic types, just check direct compatibility
        return expectedType.IsAssignableFrom(actualType);
    }

    internal class TestData
    {
        public required Func<Task<object>> TestClassInstanceFactory { get; init; }

        public required int ClassDataSourceAttributeIndex { get; init; }
        public required int ClassDataLoopIndex { get; init; }
        public required object?[] ClassData { get; init; }

        public required int MethodDataSourceAttributeIndex { get; init; }
        public required int MethodDataLoopIndex { get; init; }
        public required object?[] MethodData { get; init; }
        public required int RepeatIndex { get; init; }

        /// <summary>
        /// The depth of inheritance for this test method.
        /// 0 = method is defined directly in the test class
        /// 1 = method is inherited from immediate base class
        /// 2 = method is inherited from base's base class, etc.
        /// </summary>
        public int InheritanceDepth { get; set; } = 0;

        /// <summary>
        /// Resolved generic type arguments for the test class.
        /// Will be Type.EmptyTypes if the class is not generic.
        /// </summary>
        public Type[] ResolvedClassGenericArguments { get; set; } = Type.EmptyTypes;

        /// <summary>
        /// Resolved generic type arguments for the test method.
        /// Will be Type.EmptyTypes if the method is not generic.
        /// </summary>
        public Type[] ResolvedMethodGenericArguments { get; set; } = Type.EmptyTypes;

        /// <summary>
        /// Metadata extracted from TestDataRow wrappers or data source attributes.
        /// Contains custom DisplayName, Skip reason, and Categories.
        /// </summary>
        public TestDataRowMetadata? Metadata { get; set; }

        /// <summary>
        /// Class row objects that only a temporary instance for an instance method data source used. The
        /// test does not receive them but tracks them, so they are disposed with it (#7157).
        /// </summary>
        public object?[] UnreceivedClassRowObjects { get; init; } = [];
    }

    /// <summary>
    /// Result of attempting to create an instance for method data sources.
    /// Captures either success with an instance or failure with the exception.
    /// </summary>
    private readonly struct InstanceCreationResult
    {
        public object? Instance { get; }
        public Exception? Exception { get; }
        public bool Success => Exception == null;

        private InstanceCreationResult(object? instance, Exception? exception)
        {
            Instance = instance;
            Exception = exception;
        }

        public static InstanceCreationResult CreateSuccess(object? instance) => new(instance, null);
        public static InstanceCreationResult CreateFailure(Exception exception) => new(null, exception);
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    public async IAsyncEnumerable<AbstractExecutableTest> BuildTestsStreamingAsync(
        TestMetadata metadata,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Handle GenericTestMetadata with ConcreteInstantiations
        if (metadata is GenericTestMetadata { ConcreteInstantiations.Count: > 0 } genericMetadata)
        {
            // Stream tests from each concrete instantiation
            foreach (var concreteMetadata in genericMetadata.ConcreteInstantiations.Values)
            {
                await foreach (var test in BuildTestsStreamingAsync(concreteMetadata, cancellationToken))
                {
                    yield return test;
                }
            }
            yield break;
        }

        // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
        var repeatCount = metadata.RepeatCount ?? 0;

        // Initialize attributes
        var attributes = await InitializeAttributesAsync(metadata.GetOrCreateAttributes(), cancellationToken);

        // Create base context with ClassConstructor if present
        // StateBag and Events are lazy-initialized for performance
        var baseContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata,
            InitializedAttributes = attributes  // Store the initialized attributes
        };

        // Set the static AsyncLocal immediately so it's available for property data sources
        // This must be set BEFORE any operations that might invoke data source methods
        TestBuilderContext.Current = baseContext;

        // Check for ClassConstructor attribute and set it early if present
        // Look for any attribute that inherits from ClassConstructorAttribute
        // This handles both ClassConstructorAttribute and ClassConstructorAttribute<T>
        var classConstructorAttribute = attributes.FirstOfType<ClassConstructorAttribute>();

        if (classConstructorAttribute != null)
        {
            baseContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
        }

        var contextAccessor = new TestBuilderContextAccessor(baseContext);

        // Check for circular dependency
        if (HasInstanceDataAccessor(metadata.ClassDataSources))
        {
            yield return CreateFailedTestForClassDataSourceCircularDependency(metadata);
            yield break;
        }

        // Stream through all data source combinations
        var classDataAttributeIndex = 0;
        foreach (var classDataSource in await GetDataSourcesAsync(metadata.ClassDataSources, cancellationToken))
        {
            classDataAttributeIndex++;
            var classDataLoopIndex = 0;

            await foreach (var classDataFactory in GetInitializedDataRowsAsync(
                classDataSource,
                metadata,
                generatorType: DataGeneratorType.ClassParameters,
                testClassInstance: null,
                classInstanceArguments: null,
                contextAccessor,
                cancellationToken).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                classDataLoopIndex++;

                var (classData, firstClassRowMetadata, firstClassRowEvents) = await FetchFirstClassRowAsync(
                    classDataFactory, classDataSource, metadata, contextAccessor);

                // The first test built from this class row takes this fetch instead of calling the factory
                // again, so a non-shared source does not create an extra, untracked instance (#7157).
                var unclaimedClassRow = new UnclaimedClassRow(classData, firstClassRowMetadata, firstClassRowEvents);

                // Initialize objects before method data sources are evaluated (ObjectInitializer is phase-aware)
                await InitializeClassDataAsync(classData);

                // Handle instance creation for method data sources
                var needsInstanceForMethodDataSources = HasInstanceDataAccessor(metadata.DataSources);
                object? instanceForMethodDataSources = null;

                try
                {
                    if (needsInstanceForMethodDataSources)
                    {
                        // The temporary instance is built from the first fetch and disposed below, so no
                        // test may receive those objects: it could find them disposed. The next test
                        // tracks them instead, so they are still disposed.
                        unclaimedClassRow.LendToTemporaryInstance();

                        var instanceResult = await CreateInitializedInstanceForMethodDataSources(
                            metadata,
                            classDataAttributeIndex,
                            classDataLoopIndex,
                            classData,
                            contextAccessor.Current,
                            cancellationToken);

                        if (!instanceResult.Success)
                        {
                            // Yield a failed test instead of silently skipping
                            yield return CreateFailedTestForInstanceDataSourceError(metadata, instanceResult.Exception!, classData, unclaimedClassRow.TakeEvents());
                            continue;
                        }

                        instanceForMethodDataSources = instanceResult.Instance!;
                    }

                    // Stream through method data sources
                    var methodDataAttributeIndex = 0;
                    foreach (var methodDataSource in await GetDataSourcesAsync(metadata.DataSources, cancellationToken))
                    {
                        methodDataAttributeIndex++;
                        var methodDataLoopIndex = 0;

                        await foreach (var methodDataFactory in GetInitializedDataRowsAsync(
                            methodDataSource,
                            metadata,
                            generatorType: DataGeneratorType.TestParameters,
                            testClassInstance: methodDataSource is IAccessesInstanceData ? instanceForMethodDataSources : null,
                            classInstanceArguments: classData,
                            contextAccessor,
                            cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            methodDataLoopIndex++;

                            // Stream through repeat count
                            for (var i = 0; i < repeatCount + 1; i++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                // Build and yield single test
                                var test = await BuildSingleTestAsync(
                                    metadata, classDataFactory, unclaimedClassRow, methodDataFactory,
                                    classDataAttributeIndex, classDataLoopIndex,
                                    methodDataAttributeIndex, methodDataLoopIndex,
                                    i, contextAccessor,
                                    classDataSource, methodDataSource,
                                    cancellationToken);

                                if (test != null)
                                {
                                    yield return test;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    await DisposeInstanceForMethodDataSourcesAsync(instanceForMethodDataSources);
                }
            }
        }
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type resolution for instance creation uses reflection")]
#endif
    private async Task<InstanceCreationResult> CreateInitializedInstanceForMethodDataSources(
        TestMetadata metadata,
        int classDataAttributeIndex,
        int classDataLoopIndex,
        object?[] classData,
        TestBuilderContext testBuilderContext,
        CancellationToken cancellationToken)
    {
        var instanceResult = await CreateInstanceForMethodDataSources(
            metadata,
            classDataAttributeIndex,
            classDataLoopIndex,
            classData,
            testBuilderContext);

        if (!instanceResult.Success || instanceResult.Instance is null)
        {
            return instanceResult;
        }

        try
        {
            var tempObjectBag = new ConcurrentDictionary<string, object?>();
            var tempEvents = new TestContextEvents();

            await _objectLifecycleService.RegisterObjectAsync(
                instanceResult.Instance,
                tempObjectBag,
                metadata.MethodMetadata,
                tempEvents,
                cancellationToken);

            // Discovery: only IAsyncDiscoveryInitializer is initialized.
            await ObjectInitializer.InitializeForDiscoveryAsync(instanceResult.Instance);
        }
        catch
        {
            await DisposeInstanceForMethodDataSourcesAsync(instanceResult.Instance);
            throw;
        }

        return instanceResult;
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type resolution for instance creation uses reflection")]
#endif
    private async Task<InstanceCreationResult> CreateInstanceForMethodDataSources(
        TestMetadata metadata, int classDataAttributeIndex, int classDataLoopIndex, object?[] classData, TestBuilderContext testBuilderContext)
    {
        try
        {
            // Try ClassConstructor first - if one is configured, it handles instance creation
            var attributes = testBuilderContext.InitializedAttributes ?? metadata.GetOrCreateAttributes();
            var instance = await ClassConstructorHelper.TryCreateInstanceWithClassConstructor(
                attributes,
                metadata.TestClassType,
                testBuilderContext,
                metadata.TestSessionId);

            if (instance != null)
            {
                return InstanceCreationResult.CreateSuccess(instance);
            }

            // Fall back to InstanceFactory
            if (metadata.TestClassType.IsGenericTypeDefinition)
            {
                var tempTestData = new TestData
                {
                    TestClassInstanceFactory = () => Task.FromResult<object>(null!),
                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                    ClassDataLoopIndex = classDataLoopIndex,
                    ClassData = classData,
                    MethodDataSourceAttributeIndex = 0,
                    MethodDataLoopIndex = 0,
                    MethodData = [],
                    RepeatIndex = 0,
                    InheritanceDepth = metadata.InheritanceDepth
                };

                try
                {
                    var resolution = TestGenericTypeResolver.Resolve(metadata, tempTestData);
                    return InstanceCreationResult.CreateSuccess(metadata.InstanceFactory(resolution.ResolvedClassGenericArguments, classData));
                }
                catch (GenericTypeResolutionException) when (classData.Length == 0)
                {
                    var resolvedTypes = TryInferClassGenericsFromDataSources(metadata);
                    return InstanceCreationResult.CreateSuccess(metadata.InstanceFactory(resolvedTypes, classData));
                }
            }
            else
            {
                return InstanceCreationResult.CreateSuccess(metadata.InstanceFactory([], classData));
            }
        }
        catch (Exception ex)
        {
            return InstanceCreationResult.CreateFailure(ex);
        }
    }

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type resolution for test building uses reflection")]
#endif
    private async Task<AbstractExecutableTest?> BuildSingleTestAsync(
        TestMetadata metadata,
        Func<Task<object?[]?>> classDataFactory,
        UnclaimedClassRow unclaimedClassRow,
        Func<Task<object?[]?>> methodDataFactory,
        int classDataAttributeIndex,
        int classDataLoopIndex,
        int methodDataAttributeIndex,
        int methodDataLoopIndex,
        int repeatIndex,
        TestBuilderContextAccessor contextAccessor,
        IDataSourceAttribute? classDataSource = null,
        IDataSourceAttribute? methodDataSource = null,
        CancellationToken cancellationToken = default)
    {
        // Declared outside the try so that a row which fails after its factories ran still hands its
        // objects to the failed test, which tracks them for disposal (#7123).
        object?[]? classData = null;
        object?[]? methodData = null;

        // Fetch this test's rows against a context of their own, so the events the data sources subscribe
        // reach this test only (#7205). It shares the base context's state bag, as the streaming path always has.
        var baseContext = contextAccessor.Current;
        var iterationContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata,
            DataSourceAttribute = methodDataSource ?? baseContext.DataSourceAttribute,
            ClassDataSourceAttribute = classDataSource ?? baseContext.ClassDataSourceAttribute,
            InitializedAttributes = baseContext.InitializedAttributes,
            ClassConstructor = baseContext.ClassConstructor
        };
        iterationContext.ShareStateBagWith(baseContext);
        contextAccessor.Current = iterationContext;

        try
        {
            (classData, var classRowMetadata) = await unclaimedClassRow.TakeOrFetchAsync(classDataFactory, iterationContext);
            (methodData, var methodRowMetadata) = DataUnwrapper.UnwrapWithTypesAndMetadata(await methodDataFactory() ?? [], metadata.MethodMetadata.Parameters);

            // Extract and merge metadata from data source attributes and TestDataRow wrappers
            var classAttrMetadata = DataSourceMetadataExtractor.ExtractFromAttribute(classDataSource);
            var methodAttrMetadata = DataSourceMetadataExtractor.ExtractFromAttribute(methodDataSource);
            var mergedClassMetadata = DataSourceMetadataExtractor.Merge(classRowMetadata, classAttrMetadata);
            var mergedMethodMetadata = DataSourceMetadataExtractor.Merge(methodRowMetadata, methodAttrMetadata);
            var finalMetadata = DataSourceMetadataExtractor.Merge(mergedMethodMetadata, mergedClassMetadata);

            // Initialize method data objects (ObjectInitializer is phase-aware)
            await InitializeClassDataAsync(methodData);

            // Check data compatibility for generic methods
            if (metadata.GenericMethodTypeArguments is { Length: > 0 })
            {
                if (!IsDataCompatibleWithExpectedTypes(metadata, methodData))
                {
                    // Skip incompatible data. No test holds the class row's objects, so the next test takes them.
                    unclaimedClassRow.Return(classData, classRowMetadata, iterationContext.EventsIfCreated);
                    return null;
                }
            }

            var tempTestData = new TestData
            {
                TestClassInstanceFactory = () => Task.FromResult<object>(null!),
                ClassDataSourceAttributeIndex = classDataAttributeIndex,
                ClassDataLoopIndex = classDataLoopIndex,
                ClassData = classData,
                MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                MethodDataLoopIndex = methodDataLoopIndex,
                MethodData = methodData,
                RepeatIndex = repeatIndex,
                InheritanceDepth = metadata.InheritanceDepth
            };

            // Resolve generic types
            Type[] resolvedClassGenericArgs;
            Type[] resolvedMethodGenericArgs;

            try
            {
                var resolution = TestGenericTypeResolver.Resolve(metadata, tempTestData);
                resolvedClassGenericArgs = resolution.ResolvedClassGenericArguments;
                resolvedMethodGenericArgs = resolution.ResolvedMethodGenericArguments;
            }
            catch (GenericTypeResolutionException) when (
                metadata.TestClassType.IsGenericTypeDefinition &&
                classData.Length == 0 &&
                methodData.Length > 0)
            {
                try
                {
                    resolvedClassGenericArgs = TryInferClassGenericsFromMethodData(metadata, methodData);
                    resolvedMethodGenericArgs = Type.EmptyTypes;
                }
                catch (Exception ex)
                {
                    return CreateFailedTestForDataRowError(metadata, ex, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, repeatIndex, unclaimedClassRow.WithLentObjects(classData), methodData, iterationContext.EventsIfCreated);
                }
            }
            catch (Exception ex)
            {
                return CreateFailedTestForDataRowError(metadata, ex, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, repeatIndex, unclaimedClassRow.WithLentObjects(classData), methodData, iterationContext.EventsIfCreated);
            }

            if (metadata.TestClassType.IsGenericTypeDefinition && resolvedClassGenericArgs.Length == 0)
            {
                throw new InvalidOperationException($"Cannot create test for generic class '{metadata.TestClassType.Name}': No type arguments could be inferred. Add [GenerateGenericTest<ConcreteType>] to the class, or use a data source (like [ClassDataSource<T>] or [Arguments]) that provides constructor arguments to infer the generic type arguments from.");
            }

            // Create instance factory
            var attributes = contextAccessor.Current.InitializedAttributes ?? [];
            var basicSkipReason = GetBasicSkipReason(metadata, attributes);
            Func<Task<object>> instanceFactory;

            if (basicSkipReason is { Length: > 0 })
            {
                instanceFactory = () => Task.FromResult<object>(SkippedTestInstance.Instance);
            }
            else
            {
                var capturedMetadata = metadata;
                var capturedClassGenericArgs = resolvedClassGenericArgs;
                var capturedClassData = classData;
                var capturedContext = contextAccessor.Current;
                instanceFactory = () => CreateInstance(capturedMetadata, capturedClassGenericArgs, capturedClassData, capturedContext);
            }

            // Build final test data
            var testData = new TestData
            {
                TestClassInstanceFactory = instanceFactory,
                ClassDataSourceAttributeIndex = classDataAttributeIndex,
                ClassDataLoopIndex = classDataLoopIndex,
                ClassData = classData,
                MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                MethodDataLoopIndex = methodDataLoopIndex,
                MethodData = methodData,
                RepeatIndex = repeatIndex,
                InheritanceDepth = metadata.InheritanceDepth,
                ResolvedClassGenericArguments = resolvedClassGenericArgs,
                ResolvedMethodGenericArguments = resolvedMethodGenericArgs,
                Metadata = finalMetadata,
                UnreceivedClassRowObjects = unclaimedClassRow.TakeLentObjects()
            };

            // Events is lazy-initialized; explicitly share StateBag from per-iteration context
            var testSpecificContext = new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                ClassConstructor = contextAccessor.Current.ClassConstructor,
                DataSourceAttribute = methodDataSource ?? contextAccessor.Current.DataSourceAttribute,
                ClassDataSourceAttribute = classDataSource ?? contextAccessor.Current.ClassDataSourceAttribute,
                InitializedAttributes = attributes
            };

            // Shares the per-iteration bag lazily; it is only allocated if something reads it.
            testSpecificContext.ShareStateBagWith(contextAccessor.Current);
            testSpecificContext.ShareEventsWith(contextAccessor.Current);

            var test = await BuildTestAsync(metadata, testData, testSpecificContext, cancellationToken: cancellationToken);

            if (!string.IsNullOrEmpty(basicSkipReason))
            {
                test.Context.SkipReason = basicSkipReason;
            }

            return test;
        }
        catch (Exception ex)
        {
            // Includes a GenericConstraintViolationDataSource row whose inferred type arguments break the
            // method's constraints. The row's indexes keep its id distinct from the method's other rows.
            var (reportedException, rowData) = UnpackConstraintViolation(ex, metadata.MethodMetadata.Parameters);
            return CreateFailedTestForDataRowError(metadata, reportedException, classDataAttributeIndex, classDataLoopIndex, methodDataAttributeIndex, methodDataLoopIndex, repeatIndex, unclaimedClassRow.WithLentObjects(classData), rowData ?? methodData, iterationContext.EventsIfCreated);
        }
        finally
        {
            contextAccessor.Current = baseContext;
        }
    }

    /// <summary>
    /// Determines if a test could potentially match the filter without building the full test object.
    /// This is a conservative check - returns true unless we can definitively rule out the test.
    /// Delegates to IMetadataFilterMatcher service.
    /// </summary>
    internal bool CouldTestMatchFilter(ITestExecutionFilter filter, TestMetadata metadata)
    {
        return _filterMatcher.CouldMatchFilter(metadata, filter);
    }

    private static bool HasInstanceDataAccessor(IDataSourceAttribute[] dataSources)
    {
        foreach (var ds in dataSources)
        {
            if (ds is IAccessesInstanceData)
            {
                return true;
            }
        }
        return false;
    }

}
