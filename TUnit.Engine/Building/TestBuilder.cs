using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;
using TUnit.Core.Services;
using TUnit.Core.Tracking;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building;

internal sealed class TestBuilder : ITestBuilder
{
    private readonly string _sessionId;
    private readonly EventReceiverOrchestrator _eventReceiverOrchestrator;
    private readonly IContextProvider _contextProvider;

    public TestBuilder(string sessionId, EventReceiverOrchestrator eventReceiverOrchestrator, IContextProvider contextProvider)
    {
        _sessionId = sessionId;
        _eventReceiverOrchestrator = eventReceiverOrchestrator;
        _contextProvider = contextProvider;
    }


    public async Task<IEnumerable<AbstractExecutableTest>> BuildTestsFromMetadataAsync(TestMetadata metadata)
    {
        var tests = new List<AbstractExecutableTest>();

        try
        {
            var contextAccessor = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = metadata.MethodMetadata
            });

            if (metadata.ClassDataSources.Any(ds => ds is IAccessesInstanceData))
            {
                var failedTest = await CreateFailedTestForClassDataSourceCircularDependency(metadata);
                tests.Add(failedTest);
                return tests;
            }

            var classDataAttributeIndex = 0;
            foreach (var classDataSource in GetDataSources(metadata.ClassDataSources))
            {
                classDataAttributeIndex++;

                var classDataLoopIndex = 0;
                await foreach (var classDataFactory in classDataSource.GetDataRowsAsync(
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
                                    TestClassInstance = null!,
                                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                    ClassDataLoopIndex = classDataLoopIndex,
                                    ClassData = classData,
                                    MethodDataSourceAttributeIndex = 0,
                                    MethodDataLoopIndex = 0,
                                    MethodData = [],
                                    RepeatIndex = 0
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
                    foreach (var methodDataSource in GetDataSources(metadata.DataSources))
                    {
                        methodDataAttributeIndex++;

                        var methodDataLoopIndex = 0;
                        await foreach (var methodDataFactory in methodDataSource.GetDataRowsAsync(
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

                            for (var i = 0; i < metadata.RepeatCount + 1; i++)
                            {
                                classData = DataUnwrapper.Unwrap(await classDataFactory() ?? []);
                                var methodData = DataUnwrapper.Unwrap(await methodDataFactory() ?? []);

                                var tempTestData = new TestData
                                {
                                    TestClassInstance = null!, // Temporary placeholder
                                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                    ClassDataLoopIndex = classDataLoopIndex,
                                    ClassData = classData,
                                    MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                    MethodDataLoopIndex = methodDataLoopIndex,
                                    MethodData = methodData,
                                    RepeatIndex = i
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
                                var instance = metadata.InstanceFactory(resolvedClassGenericArgs, classData);
                                if (instance is null)
                                {
                                    throw new InvalidOperationException($"Error creating test class instance for {metadata.TestClassType.FullName}.");
                                }

                                var testData = new TestData
                                {
                                    TestClassInstance = instance,
                                    ClassDataSourceAttributeIndex = classDataAttributeIndex,
                                    ClassDataLoopIndex = classDataLoopIndex,
                                    ClassData = classData,
                                    MethodDataSourceAttributeIndex = methodDataAttributeIndex,
                                    MethodDataLoopIndex = methodDataLoopIndex,
                                    MethodData = methodData,
                                    RepeatIndex = i,
                                    ResolvedClassGenericArguments = resolvedClassGenericArgs,
                                    ResolvedMethodGenericArguments = resolvedMethodGenericArgs
                                };

                                var test = await BuildTestAsync(metadata, testData, contextAccessor.Current);
                                tests.Add(test);

                                contextAccessor.Current = new TestBuilderContext
                                {
                                    TestMetadata = metadata.MethodMetadata
                                };
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
        for (var i = 0; i < Math.Min(metadata.ParameterTypes.Length, methodData.Length); i++)
        {
            var paramType = metadata.ParameterTypes[i];
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
                    var methodParam = metadata.MethodMetadata.Parameters[i];

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

        if (metadata.DataSources.Any(ds => ds is IAccessesInstanceData))
        {
            // Look at the test method parameters to find attributes that can help with generic type inference
            foreach (var param in metadata.MethodMetadata.Parameters)
            {
                if (param.ReflectionInfo != null)
                {
                    // Get the actual parameter type from reflection
                    var actualParamType = param.ReflectionInfo.ParameterType;

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
                            for (var i = 0; i < metadata.ParameterTypes.Length; i++)
                            {
                                var paramType = metadata.ParameterTypes[i];
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

    private static IDataSourceAttribute[] GetDataSources(IDataSourceAttribute[] dataSources)
    {
        if (dataSources.Length == 0)
        {
            return [NoDataSource.Instance];
        }

        return dataSources;
    }

    public async Task<AbstractExecutableTest> BuildTestAsync(TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        var testId = TestIdentifierService.GenerateTestId(metadata, testData);

        var context = await CreateTestContextAsync(testId, metadata, testData, testBuilderContext);

        context.TestDetails.ClassInstance = testData.TestClassInstance;

        TrackDataSourceObjects(context, testData.ClassData, testData.MethodData);

        await InvokeDiscoveryEventReceiversAsync(context);

        var creationContext = new ExecutableTestCreationContext
        {
            TestId = testId,
            DisplayName = context.GetDisplayName(), // Use the display name from context which may have been updated by discovery events
            Arguments = testData.MethodData,
            ClassArguments = testData.ClassData,
            Context = context,
            ResolvedMethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ResolvedClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        return metadata.CreateExecutableTestFactory(creationContext, metadata);
    }


    private ValueTask<TestContext> CreateTestContextAsync(string testId, TestMetadata metadata, TestData testData, TestBuilderContext testBuilderContext)
    {
        var testDetails = new TestDetails
        {
            TestId = testId,
            TestName = metadata.TestName,
            ClassType = metadata.TestClassType,
            MethodName = metadata.TestMethodName,
            ClassInstance = testData.TestClassInstance,
            TestMethodArguments = testData.MethodData,
            TestClassArguments = testData.ClassData,
            TestFilePath = metadata.FilePath ?? "Unknown",
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = metadata.MethodMetadata.ReturnType ?? typeof(void),
            MethodMetadata = metadata.MethodMetadata,
            Attributes =  metadata.AttributeFactory.Invoke(),
            MethodGenericArguments = testData.ResolvedMethodGenericArguments,
            ClassGenericArguments = testData.ResolvedClassGenericArguments
        };

        var context = _contextProvider.CreateTestContext(
            metadata.TestName,
            metadata.TestClassType,
            testBuilderContext,
            CancellationToken.None);

        context.TestDetails = testDetails;

        return new ValueTask<TestContext>(context);
    }


    private async Task InvokeDiscoveryEventReceiversAsync(TestContext context)
    {
        var discoveredContext = new DiscoveredTestContext(
            context.TestDetails.TestName,
            context);

        {
            await _eventReceiverOrchestrator.InvokeTestDiscoveryEventReceiversAsync(context, discoveredContext, CancellationToken.None);
        }

        discoveredContext.TransferTo(context);
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception)
    {
        return await CreateFailedTestForDataGenerationError(metadata, exception, null);
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, string? customDisplayName)
    {
        return await CreateFailedTestForDataGenerationError(metadata, exception, new TestDataCombination(), customDisplayName);
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForDataGenerationError(TestMetadata metadata, Exception exception, TestDataCombination combination, string? customDisplayName)
    {
        var testId = TestIdentifierService.GenerateFailedTestId(metadata, combination);
        var displayName = customDisplayName ?? $"{metadata.TestClassType.Name}.{metadata.TestName}";

        var testDetails = CreateFailedTestDetails(metadata, testId);
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

    private static TestDetails CreateFailedTestDetails(TestMetadata metadata, string testId)
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
            TestLineNumber = metadata.LineNumber ?? 0,
            TestMethodParameterTypes = metadata.ParameterTypes,
            ReturnType = typeof(Task),
            MethodMetadata = metadata.MethodMetadata,
            Attributes = metadata.AttributeFactory.Invoke(),
        };
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

    private static void TrackDataSourceObjects(TestContext context, object?[] classArguments, object?[] methodArguments)
    {
        foreach (var arg in classArguments)
        {
            ObjectTracker.TrackObject(context.Events, arg);
        }

        foreach (var arg in methodArguments)
        {
            ObjectTracker.TrackObject(context.Events, arg);
        }
    }

    private async Task<AbstractExecutableTest> CreateFailedTestForInstanceDataSourceError(TestMetadata metadata, Exception exception)
    {
        var message = $"Failed to create instance for method data source expansion: {exception.Message}";
        return await CreateFailedTestForDataGenerationError(metadata, exception, message);
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
        return await CreateFailedTestForDataGenerationError(metadata, exception, message);
    }

    internal class TestData
    {
        public required object TestClassInstance { get; init; }

        public required int ClassDataSourceAttributeIndex { get; init; }
        public required int ClassDataLoopIndex { get; init; }
        public required object?[] ClassData { get; init; }

        public required int MethodDataSourceAttributeIndex { get; init; }
        public required int MethodDataLoopIndex { get; init; }
        public required object?[] MethodData { get; init; }
        public required int RepeatIndex { get; init; }

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
}
