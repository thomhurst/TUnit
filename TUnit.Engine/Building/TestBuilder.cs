using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
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
    private readonly Discovery.IHookDiscoveryService _hookDiscoveryService;
    private readonly TestArgumentRegistrationService _testArgumentRegistrationService;
    private readonly IMetadataFilterMatcher _filterMatcher;

    public TestBuilder(
        string sessionId,
        EventReceiverOrchestrator eventReceiverOrchestrator,
        IContextProvider contextProvider,
        ObjectLifecycleService objectLifecycleService,
        Discovery.IHookDiscoveryService hookDiscoveryService,
        TestArgumentRegistrationService testArgumentRegistrationService,
        IMetadataFilterMatcher filterMatcher)
    {
        _sessionId = sessionId;
        _hookDiscoveryService = hookDiscoveryService;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _contextProvider = contextProvider;
        _objectLifecycleService = objectLifecycleService;
        _testArgumentRegistrationService = testArgumentRegistrationService;
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
        var attributes = builderContext.InitializedAttributes ?? metadata.AttributeFactory();

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

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test building in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata, TestBuildingContext buildingContext)
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
            // Create a context for capturing output during test building
            using var buildContext = new TestBuildContext();
            TestBuildContext.Current = buildContext;

            // Handle GenericTestMetadata with ConcreteInstantiations
            if (metadata is GenericTestMetadata { ConcreteInstantiations.Count: > 0 } genericMetadata)
            {
                // Build tests from each concrete instantiation
                foreach (var concreteMetadata in genericMetadata.ConcreteInstantiations.Values)
                {
                    var concreteTests = await BuildTestsFromMetadataAsync(concreteMetadata, buildingContext);
                    tests.AddRange(concreteTests);
                }
                return tests;
            }


            // Use pre-extracted repeat count from metadata (avoids instantiating attributes)
            var repeatCount = metadata.RepeatCount ?? 0;

            // Create and initialize attributes ONCE
            var attributes = await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());

            if (metadata.ClassDataSources.Any(ds => ds is IAccessesInstanceData))
            {
                var failedTest = await CreateFailedTestForClassDataSourceCircularDependency(metadata);
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

            // Set the static AsyncLocal immediately so it's available for property data sources
            // This must be set BEFORE any operations that might invoke data source methods
            TestBuilderContext.Current = testBuilderContext;

            // Check for ClassConstructor attribute and set it early if present (reuse already created attributes)
            var classConstructorAttribute = attributes.OfType<ClassConstructorAttribute>().FirstOrDefault();
            if (classConstructorAttribute != null)
            {
                testBuilderContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
            }

            var contextAccessor = new TestBuilderContextAccessor(testBuilderContext);

            var classDataAttributeIndex = 0;
            foreach (var classDataSource in await GetDataSourcesAsync(metadata.ClassDataSources))
            {
                classDataAttributeIndex++;

                var classDataLoopIndex = 0;
                var hasAnyClassData = false;
                await foreach (var classDataFactory in GetInitializedDataRowsAsync(
                                   classDataSource,
                                   DataGeneratorMetadataCreator.CreateDataGeneratorMetadata
                                   (
                                       testMetadata: metadata,
                                       testSessionId: _sessionId,
                                       generatorType: DataGeneratorType.ClassParameters,
                                       testClassInstance: null, // Never pass instance for class data sources (circular dependency)
                                       classInstanceArguments: null,
                                       contextAccessor
                                   )))
                {
                    hasAnyClassData = true;
                    classDataLoopIndex++;

                    var classDataResult = await classDataFactory() ?? [];
                    var classData = DataUnwrapper.Unwrap(classDataResult);

                    // Initialize objects before method data sources are evaluated.
                    // ObjectInitializer is phase-aware and will only initialize IAsyncDiscoveryInitializer during Discovery.
                    await InitializeClassDataAsync(classData);

                    var needsInstanceForMethodDataSources = metadata.DataSources.Any(ds => ds is IAccessesInstanceData);

                    object? instanceForMethodDataSources = null;
                    var discoveryInstanceUsed = false;

                    if (needsInstanceForMethodDataSources)
                    {
                        try
                        {
                            // Try to resolve class generic types using class data for early instance creation
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
                                    instanceForMethodDataSources = metadata.InstanceFactory(resolution.ResolvedClassGenericArguments, classData);
                                }
                                catch (GenericTypeResolutionException) when (classData.Length == 0)
                                {
                                    // If we can't resolve from constructor args, try to infer from data sources
                                    var resolvedTypes = TryInferClassGenericsFromDataSources(metadata);
                                    instanceForMethodDataSources = metadata.InstanceFactory(resolvedTypes, classData);
                                }
                            }
                            else
                            {
                                // Non-generic class
                                instanceForMethodDataSources = metadata.InstanceFactory([], classData);
                            }

                            // Initialize property data sources on the early instance so that
                            // method data sources can access fully-initialized properties.
                            // This is critical for scenarios like:
                            //   [ClassDataSource<ErrFixture<T>>] public required ErrFixture<T> Fixture { get; init; }
                            //   public IEnumerable<Func<T>> TestExecutions => [() => Fixture.Value];
                            //   [MethodDataSource("TestExecutions")] [Test] public void MyTest(T value) { }
                            if (instanceForMethodDataSources != null)
                            {
                                var tempObjectBag = new ConcurrentDictionary<string, object?>();
                                var tempEvents = new TestContextEvents();

                                await _objectLifecycleService.RegisterObjectAsync(
                                    instanceForMethodDataSources,
                                    tempObjectBag,
                                    metadata.MethodMetadata,
                                    tempEvents);

                                // Discovery: only IAsyncDiscoveryInitializer is initialized
                                await ObjectInitializer.InitializeForDiscoveryAsync(instanceForMethodDataSources);
                            }
                        }
                        catch (Exception ex)
                        {
                            var failedTest = await CreateFailedTestForInstanceDataSourceError(metadata, ex);
                            tests.Add(failedTest);
                            continue;
                        }
                    }

                    var methodDataAttributeIndex = 0;
                    foreach (var methodDataSource in await GetDataSourcesAsync(metadata.DataSources))
                    {
                        methodDataAttributeIndex++;

                        var methodDataLoopIndex = 0;
                        var hasAnyMethodData = false;
                        await foreach (var methodDataFactory in GetInitializedDataRowsAsync(
                                           methodDataSource,
                                           DataGeneratorMetadataCreator.CreateDataGeneratorMetadata
                                           (
                                               testMetadata: metadata,
                                               testSessionId: _sessionId,
                                               generatorType: DataGeneratorType.TestParameters,
                                               testClassInstance: methodDataSource is IAccessesInstanceData ? instanceForMethodDataSources : null,
                                               classInstanceArguments: classData,
                                               contextAccessor
                                           )))
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
                                    InitializedAttributes = testBuilderContext.InitializedAttributes,  // Preserve attributes from parent context
                                    ClassConstructor = testBuilderContext.ClassConstructor  // Preserve ClassConstructor for instance creation
                                };

                                var (classDataUnwrapped, classRowMetadata) = DataUnwrapper.UnwrapWithMetadata(await classDataFactory() ?? []);
                                classData = classDataUnwrapped;
                                var (methodData, methodRowMetadata) = DataUnwrapper.UnwrapWithTypesAndMetadata(await methodDataFactory() ?? [], metadata.MethodMetadata.Parameters);

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
                                        // Skip this data source as it's not compatible with the expected types
                                        continue;
                                    }
                                }

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
                                        var failedTest = await CreateFailedTestForDataGenerationError(metadata, innerEx);
                                        tests.Add(failedTest);
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // If generic resolution fails, create a failed test
                                    var failedTest = await CreateFailedTestForDataGenerationError(metadata, ex);
                                    tests.Add(failedTest);
                                    continue;
                                }

                                if (metadata.TestClassType.IsGenericTypeDefinition && resolvedClassGenericArgs.Length == 0)
                                {
                                    throw new InvalidOperationException($"Cannot create test for generic class '{metadata.TestClassType.Name}': No type arguments could be inferred. Add [GenerateGenericTest<ConcreteType>] to the class, or use a data source (like [ClassDataSource<T>] or [Arguments]) that provides constructor arguments to infer the generic type arguments from.");
                                }

                                var basicSkipReason = GetBasicSkipReason(metadata, attributes);

                                Func<Task<object>> instanceFactory;
                                var isReusingDiscoveryInstance = false;

                                if (basicSkipReason is { Length: > 0 })
                                {
                                    instanceFactory = () => Task.FromResult<object>(SkippedTestInstance.Instance);
                                }
                                else if (methodDataLoopIndex == 1 && i == 0 && instanceForMethodDataSources != null && !discoveryInstanceUsed)
                                {
                                    // Reuse the discovery instance for the first test to avoid duplicate initialization
                                    var capturedInstance = instanceForMethodDataSources;
                                    discoveryInstanceUsed = true;
                                    isReusingDiscoveryInstance = true;

                                    instanceFactory = () => Task.FromResult(capturedInstance);
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
                                    Metadata = finalMetadata
                                };

                                // Events is lazy-initialized; explicitly share StateBag from per-iteration context
                                var testSpecificContext = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata,
                                    StateBag = contextAccessor.Current.StateBag,
                                    ClassConstructor = testBuilderContext.ClassConstructor,
                                    DataSourceAttribute = contextAccessor.Current.DataSourceAttribute,
                                    InitializedAttributes = attributes
                                };

                                var test = await BuildTestAsync(metadata, testData, testSpecificContext, isReusingDiscoveryInstance);

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
                                var failedTest = await CreateFailedTestForDataGenerationError(metadata, genericResolutionException);
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
                                    ResolvedMethodGenericArguments = Type.EmptyTypes
                                };

                                // StateBag and Events are lazy-initialized
                                var testSpecificContext = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata,
                                    ClassConstructor = testBuilderContext.ClassConstructor,
                                    DataSourceAttribute = methodDataSource,
                                    InitializedAttributes = attributes
                                };

                                var test = await BuildTestAsync(metadata, testData, testSpecificContext);
                                test.Context.SkipReason = skipReason;
                                tests.Add(test);
                            }
                        }
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
                        var failedTest = await CreateFailedTestForDataGenerationError(metadata, genericResolutionException);
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

                        // StateBag and Events are lazy-initialized
                        var testSpecificContext = new TestBuilderContext
                        {
                            TestMetadata = metadata.MethodMetadata,
                            ClassConstructor = testBuilderContext.ClassConstructor,
                            DataSourceAttribute = classDataSource,
                            InitializedAttributes = attributes
                        };

                        var test = await BuildTestAsync(metadata, testData, testSpecificContext);
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
                foreach (var test in tests)
                {
                    test.Context.SetBuildTimeOutput(capturedOutput, capturedErrorOutput);
                }
            }
        }
        catch (Exception ex)
        {
            var failedTest = await CreateFailedTestForDataGenerationError(metadata, ex);
            tests.Add(failedTest);
            return tests;
        }

        return tests;
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

#if NET6_0_OR_GREATER
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

        if (metadata.DataSources.Any(ds => ds is IAccessesInstanceData))
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

    private async Task<IDataSourceAttribute[]> GetDataSourcesAsync(IDataSourceAttribute[] dataSources)
    {
        if (dataSources.Length == 0)
        {
            return _dataSourceArray;
        }

        // Inject properties into data sources during discovery (IAsyncInitializer deferred to execution)
        foreach (var dataSource in dataSources)
        {
            await _objectLifecycleService.InjectPropertiesAsync(dataSource);
        }

        return dataSources;
    }

    /// <summary>
    /// Ensures a data source is initialized before use and returns data rows.
    /// This centralizes the initialization logic for all data source usage.
    /// </summary>
    private async IAsyncEnumerable<Func<Task<object?[]?>>> GetInitializedDataRowsAsync(
        IDataSourceAttribute dataSource,
        DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Inject properties into data source during discovery (IAsyncInitializer deferred to execution)
        var propertyInjectedDataSource = await _objectLifecycleService.InjectPropertiesAsync(
            dataSource,
            dataGeneratorMetadata.TestBuilderContext.Current.StateBag,
            dataGeneratorMetadata.TestInformation,
            dataGeneratorMetadata.TestBuilderContext.Current.Events);

        // Now get data rows from the property-injected data source
        await foreach (var dataRow in propertyInjectedDataSource.GetDataRowsAsync(dataGeneratorMetadata))
        {
            yield return dataRow;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hook discovery service handles mode-specific logic; reflection calls suppressed in AOT mode")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Hook discovery service handles mode-specific logic; dynamic code suppressed in AOT mode")]
    public async Task<AbstractExecutableTest> BuildTestAsync(TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext, bool isReusingDiscoveryInstance = false)
    {
        // Discover instance hooks for closed generic types (no-op in source gen mode)
        if (metadata.TestClassType is { IsGenericType: true, IsGenericTypeDefinition: false })
        {
            _hookDiscoveryService.DiscoverInstanceHooksForType(metadata.TestClassType);
        }

        var testId = TestIdentifierService.GenerateTestId(metadata, testData);

        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext);

        // Mark if this test is reusing the discovery instance (already initialized)
        context.IsDiscoveryInstanceReused = isReusingDiscoveryInstance;

        context.Metadata.TestDetails.ClassInstance = PlaceholderInstance.Instance;

        // Apply metadata from TestDataRow or data source attributes
        if (testData.Metadata is { } dataRowMetadata)
        {
            // Apply custom display name (will be processed by DisplayNameBuilder)
            if (!string.IsNullOrEmpty(dataRowMetadata.DisplayName))
            {
                context.SetDataSourceDisplayName(dataRowMetadata.DisplayName!);
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

        // Register test arguments for property injection and reference counting
        // Note: ITestRegisteredEventReceiver and ITestDiscoveryEventReceiver are invoked later
        // in InvokePostResolutionEventsAsync after dependencies are resolved
        try
        {
            await RegisterTestArgumentsAsync(context);
        }
        catch (Exception ex)
        {
            // Property registration failed - mark the test as failed immediately
            test.SetResult(TestState.Failed, ex);
        }
        finally
        {
            // Clear TestContext.Current so subsequent build operations use TestBuildContext.Current
            // for output capture. This ensures console output during data source evaluation
            // goes to the shared build context, not a previous test's context.
            TestContext.Current = null;
        }

        return test;
    }

    /// <summary>
    /// Invokes event receivers after dependencies have been resolved.
    /// This is called from TestDiscoveryService after all tests are built and dependencies resolved.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public async ValueTask InvokePostResolutionEventsAsync(AbstractExecutableTest test)
    {
        var context = test.Context;

        // Set TestContext.Current so output capture works via AsyncLocal
        // This ensures any console output during event receiver invocation is captured
        TestContext.Current = context;

        // Populate TestContext._dependencies from resolved test.Dependencies
        // This makes dependencies available to event receivers
        PopulateDependencies(test, context._dependencies);

        // Invoke discovery event receivers first (discovery phase)
        await InvokeDiscoveryEventReceiversAsync(context);

        // Invoke test registered event receivers (registration phase)
        try
        {
            await InvokeTestRegisteredReceiversAsync(context);
        }
        catch (Exception ex)
        {
            // Registration logic failed - mark the test as failed
            test.SetResult(TestState.Failed, ex);
        }

        // Clear the cached display name after registration events
        // This ensures that ArgumentDisplayFormatterAttribute and similar attributes
        // have a chance to register their formatters before the display name is finalized
        context.InvalidateDisplayNameCache();
    }

    private static void PopulateDependencies(AbstractExecutableTest test, List<TestDetails> dependencies)
    {
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
        var attributes = cachedAttributes ?? metadata.AttributeFactory();

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
        // Return the first reason (they all should skip), or null if no skip attributes
        return firstSkipAttribute?.Reason;
    }

    private async ValueTask<TestContext> CreateTestContextAsync(string testId, TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        // Use attributes from context if available, or create new ones
        var attributes = testBuilderContext.InitializedAttributes ?? await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());

        if (testBuilderContext.DataSourceAttribute != null && testBuilderContext.DataSourceAttribute is not NoDataSource)
        {
            attributes = [..attributes, (Attribute)testBuilderContext.DataSourceAttribute];
        }

        var testDetails = new TestDetails(attributes)
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = PlaceholderInstance.Instance,
            TestMethodArguments = testData.MethodData,
            TestClassArguments = testData.ClassData,
            TestFilePath = metadata.FilePath,
            TestLineNumber = metadata.LineNumber,
            ReturnType = metadata.MethodMetadata.ReturnType ?? typeof(void),
            MethodMetadata = metadata.MethodMetadata,
            AttributesByType = attributes.ToAttributeDictionary(),
            MethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ClassGenericArguments = testData.ResolvedClassGenericArguments,
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
            // Don't set RetryLimit here - let discovery event receivers set it
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            testBuilderContext,
            CancellationToken.None);

        context.Metadata.TestDetails = testDetails;

        return context;
    }

    /// <summary>
    /// Registers test arguments for property injection and reference counting.
    /// Called during test building, before dependencies are resolved.
    /// </summary>
    private async Task RegisterTestArgumentsAsync(TestContext context)
    {
        var discoveredTest = new DiscoveredTest<object>
        {
            TestContext = context
        };

        context.InternalDiscoveredTest = discoveredTest;

        // Invoke the global test argument registration service to register shared instances
        await _testArgumentRegistrationService.RegisterTestArgumentsAsync(context);
    }

    /// <summary>
    /// Invokes ITestRegisteredEventReceiver receivers.
    /// Called after dependencies are resolved so receivers can access dependency information.
    /// </summary>
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    private async Task InvokeTestRegisteredReceiversAsync(TestContext context)
    {
        var registeredContext = new TestRegisteredContext(context)
        {
            // InternalDiscoveredTest is set in RegisterTestArgumentsAsync during building
            DiscoveredTest = context.InternalDiscoveredTest!
        };

        // Use pre-computed receivers (already filtered, sorted, and scoped-attribute filtered)
        foreach (var receiver in context.GetTestRegisteredReceivers())
        {
            await receiver.OnTestRegistered(registeredContext);
        }
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Scoped attribute filtering uses Type.GetInterfaces and reflection")]
#endif
    private Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.Metadata.TestDetails.TestName,
            context);

        return _eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        return await CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination());
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, TestDataCombination combination)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata, combination);

        var testDetails = await CreateFailedTestDetails(metadata, testId);
        var context = CreateFailedTestContext(metadata, testDetails);

        return new FailedExecutableTest(exception)
        {
            TestId = testId,
            Metadata = metadata,
            Arguments = [],
            ClassArguments = [],
            Context = context
        };
    }

    private async Task<TestDetails> CreateFailedTestDetails(TestMetadata metadata, string testId)
    {
        var attributes = (await InitializeAttributesAsync(metadata.AttributeFactory.Invoke()));
        return new TestDetails(attributes)
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = null!,
            TestMethodArguments = [],
            TestClassArguments = [],
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            AttributesByType = attributes.ToAttributeDictionary(),
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
        };
    }

    private async Task<Attribute[]> InitializeAttributesAsync(Attribute[] attributes)
    {
        // Inject properties into data source attributes during discovery
        // IAsyncInitializer.InitializeAsync is deferred to execution time
        foreach (var attribute in attributes)
        {
            if (attribute is IDataSourceAttribute dataSource)
            {
                await _objectLifecycleService.InjectPropertiesAsync(dataSource);
            }
        }

        return attributes;
    }

    private TestContext CreateFailedTestContext(TestMetadata metadata, TestDetails testDetails)
    {
        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            },
            CancellationToken.None);

        context.Metadata.TestDetails = testDetails;

        return context;
    }



    private async Task<AbstractExecutableTest> CreateFailedTestForInstanceDataSourceError(TestMetadata metadata, Exception exception)
    {
        var message = $"Failed to create instance for method data source expansion: {exception.Message}";
        return await CreateFailedTestForDataGenerationError(metadata, exception);
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForClassDataSourceCircularDependency(TestMetadata metadata)
    {
        var instanceClassDataSources = metadata.ClassDataSources
            .Where(ds => ds is IAccessesInstanceData)
            .Select(ds => ds.GetType().Name)
            .ToList();

        var dataSourceNames = string.Join(", ", instanceClassDataSources);
        var genericParams = string.Join(", ", metadata.TestClassType.GetGenericArguments().Select(t => t.Name));

        var message = $"Cannot use instance method data sources ({dataSourceNames}) for class constructor arguments with generic test class '{metadata.TestClassType.Name}<{genericParams}>'. " +
                      "This creates a circular dependency: instance data sources need an instance, but the instance needs constructor arguments from class data sources. " +
                      "Consider using static method data sources for class constructor arguments instead.";

        var exception = new InvalidOperationException(message);
        return await CreateFailedTestForDataGenerationError(metadata, exception);
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

#if NET6_0_OR_GREATER
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

#if NET6_0_OR_GREATER
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
        var attributes = await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());

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
        var classConstructorAttribute = attributes
            .Where(a => a is ClassConstructorAttribute)
            .Cast<ClassConstructorAttribute>()
            .FirstOrDefault();

        if (classConstructorAttribute != null)
        {
            baseContext.ClassConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;
        }

        var contextAccessor = new TestBuilderContextAccessor(baseContext);

        // Check for circular dependency
        if (metadata.ClassDataSources.Any(ds => ds is IAccessesInstanceData))
        {
            yield return await CreateFailedTestForClassDataSourceCircularDependency(metadata);
            yield break;
        }

        // Stream through all data source combinations
        var classDataAttributeIndex = 0;
        foreach (var classDataSource in await GetDataSourcesAsync(metadata.ClassDataSources))
        {
            classDataAttributeIndex++;
            var classDataLoopIndex = 0;

            await foreach (var classDataFactory in GetInitializedDataRowsAsync(
                               classDataSource,
                               DataGeneratorMetadataCreator.CreateDataGeneratorMetadata(
                                   testMetadata: metadata,
                                   testSessionId: _sessionId,
                                   generatorType: DataGeneratorType.ClassParameters,
                                   testClassInstance: null,
                                   classInstanceArguments: null,
                                   contextAccessor)).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                classDataLoopIndex++;

                var classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);

                // Initialize objects before method data sources are evaluated (ObjectInitializer is phase-aware)
                await InitializeClassDataAsync(classData);

                // Handle instance creation for method data sources
                var needsInstanceForMethodDataSources = metadata.DataSources.Any(ds => ds is IAccessesInstanceData);
                object? instanceForMethodDataSources = null;

                if (needsInstanceForMethodDataSources)
                {
                    var instanceResult = await CreateInstanceForMethodDataSources(
                        metadata, classDataAttributeIndex, classDataLoopIndex, classData);

                    if (!instanceResult.Success)
                    {
                        // Yield a failed test instead of silently skipping
                        yield return await CreateFailedTestForInstanceDataSourceError(metadata, instanceResult.Exception!);
                        continue;
                    }

                    instanceForMethodDataSources = instanceResult.Instance!;

                    // Initialize property data sources on the early instance so that
                    // method data sources can access fully-initialized properties.
                    var tempObjectBag = new ConcurrentDictionary<string, object?>();
                    var tempEvents = new TestContextEvents();

                    await _objectLifecycleService.RegisterObjectAsync(
                        instanceForMethodDataSources,
                        tempObjectBag,
                        metadata.MethodMetadata,
                        tempEvents);

                    // Discovery: only IAsyncDiscoveryInitializer is initialized
                    await ObjectInitializer.InitializeForDiscoveryAsync(instanceForMethodDataSources);
                }

                // Stream through method data sources
                var methodDataAttributeIndex = 0;
                foreach (var methodDataSource in await GetDataSourcesAsync(metadata.DataSources))
                {
                    methodDataAttributeIndex++;
                    var methodDataLoopIndex = 0;

                    await foreach (var methodDataFactory in GetInitializedDataRowsAsync(
                        methodDataSource,
                        DataGeneratorMetadataCreator.CreateDataGeneratorMetadata(
                            testMetadata: metadata,
                            testSessionId: _sessionId,
                            generatorType: DataGeneratorType.TestParameters,
                            testClassInstance: methodDataSource is IAccessesInstanceData ? instanceForMethodDataSources : null,
                            classInstanceArguments: classData,
                            contextAccessor)))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        methodDataLoopIndex++;

                        // Stream through repeat count
                        for (var i = 0; i < repeatCount + 1; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Build and yield single test
                            var test = await BuildSingleTestAsync(
                                metadata, classDataFactory, methodDataFactory,
                                classDataAttributeIndex, classDataLoopIndex,
                                methodDataAttributeIndex, methodDataLoopIndex,
                                i, contextAccessor,
                                classDataSource, methodDataSource);

                            if (test != null)
                            {
                                yield return test;
                            }
                        }
                    }
                }
            }
        }
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type resolution for instance creation uses reflection")]
#endif
    private Task<InstanceCreationResult> CreateInstanceForMethodDataSources(
        TestMetadata metadata, int classDataAttributeIndex, int classDataLoopIndex, object?[] classData)
    {
        try
        {
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
                    return Task.FromResult(InstanceCreationResult.CreateSuccess(metadata.InstanceFactory(resolution.ResolvedClassGenericArguments, classData)));
                }
                catch (GenericTypeResolutionException) when (classData.Length == 0)
                {
                    var resolvedTypes = TryInferClassGenericsFromDataSources(metadata);
                    return Task.FromResult(InstanceCreationResult.CreateSuccess(metadata.InstanceFactory(resolvedTypes, classData)));
                }
            }
            else
            {
                return Task.FromResult(InstanceCreationResult.CreateSuccess(metadata.InstanceFactory([], classData)));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(InstanceCreationResult.CreateFailure(ex));
        }
    }

#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Generic type resolution for test building uses reflection")]
#endif
    private async Task<AbstractExecutableTest?> BuildSingleTestAsync(
        TestMetadata metadata,
        Func<Task<object?[]?>> classDataFactory,
        Func<Task<object?[]?>> methodDataFactory,
        int classDataAttributeIndex,
        int classDataLoopIndex,
        int methodDataAttributeIndex,
        int methodDataLoopIndex,
        int repeatIndex,
        TestBuilderContextAccessor contextAccessor,
        IDataSourceAttribute? classDataSource = null,
        IDataSourceAttribute? methodDataSource = null)
    {
        try
        {
            var (classData, classRowMetadata) = DataUnwrapper.UnwrapWithMetadata(await classDataFactory() ?? []);
            var (methodData, methodRowMetadata) = DataUnwrapper.UnwrapWithTypesAndMetadata(await methodDataFactory() ?? [], metadata.MethodMetadata.Parameters);

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
                    return null; // Skip incompatible data
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
                    return await CreateFailedTestForDataGenerationError(metadata, ex);
                }
            }
            catch (Exception ex)
            {
                return await CreateFailedTestForDataGenerationError(metadata, ex);
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
                Metadata = finalMetadata
            };

            // Events is lazy-initialized; explicitly share StateBag from per-iteration context
            var testSpecificContext = new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                StateBag = contextAccessor.Current.StateBag,
                ClassConstructor = contextAccessor.Current.ClassConstructor,
                DataSourceAttribute = contextAccessor.Current.DataSourceAttribute,
                InitializedAttributes = attributes
            };

            var test = await BuildTestAsync(metadata, testData, testSpecificContext);

            if (!string.IsNullOrEmpty(basicSkipReason))
            {
                test.Context.SkipReason = basicSkipReason;
            }

            return test;
        }
        catch (Exception ex)
        {
            return await CreateFailedTestForDataGenerationError(metadata, ex);
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
}
