using TUnit.Core;
using TUnit.Core.DataSources;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Building;

internal sealed class TestBuilder : ITestBuilder
{
    private readonly string _sessionId;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly IContextProvider _contextProvider;
    private readonly PropertyInjectionService _propertyInjectionService;
    private readonly DataSourceInitializer _dataSourceInitializer;

    public TestBuilder(
        string sessionId, 
        EventReceiverOrchestrator eventReceiverOrchestrator, 
        IContextProvider contextProvider,
        PropertyInjectionService propertyInjectionService,
        DataSourceInitializer dataSourceInitializer)
    {
        _sessionId = sessionId;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _contextProvider = contextProvider;
        _propertyInjectionService = propertyInjectionService;
        _dataSourceInitializer = dataSourceInitializer;
    }

    /// <summary>
    /// Initializes any IAsyncInitializer objects in class data that were deferred during registration.
    /// </summary>
    private async Task InitializeDeferredClassDataAsync(object?[] classData)
    {
        if (classData == null || classData.Length == 0)
        {
            return;
        }

        foreach (var data in classData)
        {
            if (data is IAsyncInitializer asyncInitializer && data is not IDataSourceAttribute)
            {
                if (!ObjectInitializer.IsInitialized(data))
                {
                    await ObjectInitializer.InitializeAsync(data);
                }
            }
        }
    }

    private async Task<object> CreateInstance(TestMetadata metadata, Type[] resolvedClassGenericArgs, object?[] classData, TestBuilderContext builderContext)
    {
        // Initialize any deferred IAsyncInitializer objects in class data
        await InitializeDeferredClassDataAsync(classData);

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

    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<AbstractExecutableTest>();

        try
        {
            // Handle GenericTestMetadata with ConcreteInstantiations
            if (metadata is GenericTestMetadata { ConcreteInstantiations.Count: > 0 } genericMetadata)
            {
                // Build tests from each concrete instantiation
                foreach (var concreteMetadata in genericMetadata.ConcreteInstantiations.Values)
                {
                    var concreteTests = await BuildTestsFromMetadataAsync(concreteMetadata);
                    tests.AddRange(concreteTests);
                }
                return tests;
            }


            // Create and initialize attributes ONCE
            var attributes = await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());
            var filteredAttributes = ScopedAttributeFilter.FilterScopedAttributes(attributes);
            var repeatAttr = filteredAttributes.OfType<RepeatAttribute>().FirstOrDefault();
            var repeatCount = repeatAttr?.Times ?? 0;

            if (metadata.ClassDataSources.Any(ds => ds is IAccessesInstanceData))
            {
                var failedTest = await CreateFailedTestForClassDataSourceCircularDependency(metadata);
                tests.Add(failedTest);
                return tests;
            }

            // Create a single context accessor that we'll reuse, updating its Current property for each test
            var testBuilderContext = new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>(),
                InitializedAttributes = attributes  // Store the initialized attributes
            };
            
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
                    classDataLoopIndex++;

                    var classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);

                    var needsInstanceForMethodDataSources = metadata.DataSources.Any(ds => ds is IAccessesInstanceData);

                    object? instanceForMethodDataSources = null;

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
                            methodDataLoopIndex++;

                            for (var i = 0; i < repeatCount + 1; i++)
                            {
                                // Update context BEFORE calling data factories so they track objects in the right context
                                contextAccessor.Current = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata,
                                    Events = new TestContextEvents(),
                                    ObjectBag = new Dictionary<string, object?>()
                                };

                                classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);
                                var methodData = DataUnwrapper.UnwrapWithTypes(await methodDataFactory() ?? [], metadata.MethodMetadata.Parameters);

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
                                    throw new InvalidOperationException($"Cannot create instance of generic type {metadata.TestClassType.Name} with empty type arguments");
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
                                    ResolvedMethodGenericArguments = resolvedMethodGenericArgs
                                };

                                // Create a unique TestBuilderContext for this specific test
                                var testSpecificContext = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata,
                                    Events = new TestContextEvents(),
                                    ObjectBag = new Dictionary<string, object?>(),
                                    ClassConstructor = testBuilderContext.ClassConstructor, // Copy the ClassConstructor from the template
                                    DataSourceAttribute = contextAccessor.Current.DataSourceAttribute, // Copy any data source attribute
                                    InitializedAttributes = attributes // Pass the initialized attributes
                                };
                                
                                var test = await BuildTestAsync(metadata, testData, testSpecificContext);

                                // If we have a basic skip reason, set it immediately
                                if (!string.IsNullOrEmpty(basicSkipReason))
                                {
                                    test.Context.SkipReason = basicSkipReason;
                                }
                                tests.Add(test);

                                // Context already updated at the beginning of the loop before calling factories
                            }
                        }
                    }
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

                    if (methodParam.TypeReference is { IsGenericParameter: true, IsMethodGenericParameter: false })
                    {
                        var genericParamName = methodParam.TypeReference.GenericParameterName;
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

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern",
        Justification = "Data sources require reflection to get method and parameter info")]
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
                                    if (methodParam.TypeReference is { IsGenericParameter: true, IsMethodGenericParameter: false })
                                    {
                                        var genericParamName = methodParam.TypeReference.GenericParameterName;
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

    private async Task<IDataSourceAttribute[]> GetDataSourcesAsync(IDataSourceAttribute[] dataSources)
    {
        if (dataSources.Length == 0)
        {
            return [NoDataSource.Instance];
        }

        // Initialize all data sources to ensure properties are injected
        foreach (var dataSource in dataSources)
        {
            await _dataSourceInitializer.EnsureInitializedAsync(dataSource);
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
        // Ensure the data source is fully initialized before getting data rows
        // This includes property injection and IAsyncInitializer.InitializeAsync
        var initializedDataSource = await _dataSourceInitializer.EnsureInitializedAsync(
            dataSource,
            dataGeneratorMetadata.TestBuilderContext?.Current.ObjectBag,
            dataGeneratorMetadata.TestInformation,
            dataGeneratorMetadata.TestBuilderContext?.Current.Events);

        // Now get data rows from the initialized data source
        await foreach (var dataRow in initializedDataSource.GetDataRowsAsync(dataGeneratorMetadata))
        {
            yield return dataRow;
        }
    }

    public async Task<AbstractExecutableTest> BuildTestAsync(TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        var testId = TestIdentifierService.GenerateTestId(metadata, testData);

        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext);

        context.TestDetails.ClassInstance = PlaceholderInstance.Instance;

        // Arguments will be tracked by TestArgumentTrackingService during TestRegistered event
        // This ensures proper reference counting for shared instances

        await InvokeDiscoveryEventReceiversAsync(context);

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

        return metadata.CreateExecutableTestFactory(creationContext, metadata);
    }

    /// <summary>
    /// Checks if a test has basic SkipAttribute instances that can be evaluated at discovery time.
    /// Returns null if no skip attributes, a skip reason if basic skip attributes are found,
    /// or empty string if custom skip attributes requiring runtime evaluation are found.
    /// </summary>
    private static string? GetBasicSkipReason(TestMetadata metadata, Attribute[]? cachedAttributes = null)
    {
        var attributes = cachedAttributes ?? metadata.AttributeFactory();
        var skipAttributes = attributes.OfType<SkipAttribute>().ToList();

        if (skipAttributes.Count == 0)
        {
            return null; // No skip attributes
        }

        // Check if all skip attributes are basic (non-derived) SkipAttribute instances
        foreach (var skipAttribute in skipAttributes)
        {
            var attributeType = skipAttribute.GetType();
            if (attributeType != typeof(SkipAttribute))
            {
                // This is a derived skip attribute that might have custom ShouldSkip logic
                return string.Empty; // Indicates custom skip attributes that need runtime evaluation
            }
        }

        // All skip attributes are basic SkipAttribute instances
        // Return the first reason (they all should skip)
        return skipAttributes[0].Reason;
    }


    private async ValueTask<TestContext> CreateTestContextAsync(string testId, TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        // Use attributes from context if available, or create new ones
        var attributes = testBuilderContext.InitializedAttributes ?? await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());

        var testDetails = new TestDetails
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
            Attributes = attributes,
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

        context.TestDetails = testDetails;

        return context;
    }


    private async Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context);

        {
            await _eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);
        }
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

        await InvokeDiscoveryEventReceiversAsync(context);

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
        return new TestDetails
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
            Attributes = await InitializeAttributesAsync(metadata.AttributeFactory.Invoke()),
            Timeout = TimeSpan.FromMinutes(30) // Default 30-minute timeout (can be overridden by TimeoutAttribute)
        };
    }

    private async Task<Attribute[]> InitializeAttributesAsync(Attribute[] attributes)
    {
        // Initialize any attributes that need property injection or implement IAsyncInitializer
        // This ensures they're fully initialized before being used
        foreach (var attribute in attributes)
        {
            if (attribute is IDataSourceAttribute dataSource)
            {
                // Data source attributes need to be initialized with property injection
                await _dataSourceInitializer.EnsureInitializedAsync(dataSource);
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

        context.TestDetails = testDetails;

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

    private static Type? GetExpectedTypeForParameter(ParameterMetadata param, Type[] genericTypeArgs)
    {
        if (param.TypeReference == null)
        {
            return null;
        }

        // If it's a direct generic parameter (e.g., T)
        if (param.TypeReference.IsGenericParameter)
        {
            var position = param.TypeReference.GenericParameterPosition;
            if (position < genericTypeArgs.Length)
            {
                return genericTypeArgs[position];
            }
        }

        // For constructed generic types, we'll just return the element type for now
        // and let IsTypeCompatible handle the full type checking
        if (param.TypeReference.GenericArguments?.Count > 0)
        {
            // For now, check the first type argument
            var firstTypeArg = param.TypeReference.GenericArguments[0];
            if (firstTypeArg.IsGenericParameter)
            {
                var position = firstTypeArg.GenericParameterPosition;
                if (position < genericTypeArgs.Length)
                {
                    // Return the element type - we'll check compatibility in IsTypeCompatible
                    return genericTypeArgs[position];
                }
            }
        }

        return null;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL2070:UnrecognizedReflectionPattern",
        Justification = "Type checking at runtime is required for data source filtering")]
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
    }

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

        // Extract repeat count from attributes
        var attributes = await InitializeAttributesAsync(metadata.AttributeFactory.Invoke());
        var filteredAttributes = ScopedAttributeFilter.FilterScopedAttributes(attributes);
        var repeatAttr = filteredAttributes.OfType<RepeatAttribute>().FirstOrDefault();
        var repeatCount = repeatAttr?.Times ?? 0;

        // Create base context with ClassConstructor if present
        var baseContext = new TestBuilderContext
        {
            TestMetadata = metadata.MethodMetadata,
            Events = new TestContextEvents(),
            ObjectBag = new Dictionary<string, object?>(),
            InitializedAttributes = attributes  // Store the initialized attributes
        };
        
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
                    contextAccessor)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                classDataLoopIndex++;

                var classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);

                // Handle instance creation for method data sources
                var needsInstanceForMethodDataSources = metadata.DataSources.Any(ds => ds is IAccessesInstanceData);
                object? instanceForMethodDataSources = null;

                if (needsInstanceForMethodDataSources)
                {
                    instanceForMethodDataSources = await CreateInstanceForMethodDataSources(
                        metadata, classDataAttributeIndex, classDataLoopIndex, classData);

                    if (instanceForMethodDataSources == null)
                    {
                        continue; // Skip if instance creation failed
                    }
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
                                i, contextAccessor);

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

    private Task<object?> CreateInstanceForMethodDataSources(
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
                    return Task.FromResult<object?>(metadata.InstanceFactory(resolution.ResolvedClassGenericArguments, classData));
                }
                catch (GenericTypeResolutionException) when (classData.Length == 0)
                {
                    var resolvedTypes = TryInferClassGenericsFromDataSources(metadata);
                    return Task.FromResult<object?>(metadata.InstanceFactory(resolvedTypes, classData));
                }
            }
            else
            {
                return Task.FromResult<object?>(metadata.InstanceFactory([], classData));
            }
        }
        catch
        {
            return Task.FromResult<object?>(null);
        }
    }

    private async Task<AbstractExecutableTest?> BuildSingleTestAsync(
        TestMetadata metadata,
        Func<Task<object?[]?>> classDataFactory,
        Func<Task<object?[]?>> methodDataFactory,
        int classDataAttributeIndex,
        int classDataLoopIndex,
        int methodDataAttributeIndex,
        int methodDataLoopIndex,
        int repeatIndex,
        TestBuilderContextAccessor contextAccessor)
    {
        try
        {
            var classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);
            
            var methodData = DataUnwrapper.UnwrapWithTypes(await methodDataFactory() ?? [], metadata.MethodMetadata.Parameters);

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
                throw new InvalidOperationException($"Cannot create instance of generic type {metadata.TestClassType.Name} with empty type arguments");
            }

            // Create instance factory
            var attributes = contextAccessor.Current.InitializedAttributes ?? Array.Empty<Attribute>();
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
                ResolvedMethodGenericArguments = resolvedMethodGenericArgs
            };

            // Create a unique TestBuilderContext for this specific test
            var testSpecificContext = new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>(),
                ClassConstructor = contextAccessor.Current.ClassConstructor, // Preserve ClassConstructor if it was set
                DataSourceAttribute = contextAccessor.Current.DataSourceAttribute, // Preserve data source attribute
                InitializedAttributes = attributes // Pass the initialized attributes
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
}
