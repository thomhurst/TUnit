using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Interfaces;
using Expression = System.Linq.Expressions.Expression;

namespace TUnit.Engine.Services;

/// <summary>
/// Service for registering and managing dynamically added tests during runtime execution.
/// </summary>
internal sealed class TestRegistry : ITestRegistry
{
    private readonly ConcurrentQueue<PendingDynamicTest> _pendingTests = new();
    private readonly TestBuilderPipeline? _testBuilderPipeline;
    private readonly ITestCoordinator _testCoordinator;
    private readonly IDynamicTestQueue _dynamicTestQueue;
    private readonly CancellationToken _sessionCancellationToken;
    private readonly string? _sessionId;

    public TestRegistry(TestBuilderPipeline testBuilderPipeline,
        ITestCoordinator testCoordinator,
        IDynamicTestQueue dynamicTestQueue,
        string sessionId,
        CancellationToken sessionCancellationToken)
    {
        _testBuilderPipeline = testBuilderPipeline;
        _testCoordinator = testCoordinator;
        _dynamicTestQueue = dynamicTestQueue;
        _sessionId = sessionId;
        _sessionCancellationToken = sessionCancellationToken;
    }

    [RequiresUnreferencedCode("Adding dynamic tests requires reflection which is not supported in native AOT scenarios.")]
    public async Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(TestContext context, DynamicTest<T> dynamicTest) where T : class
    {
        // Create a dynamic test discovery result
        var discoveryResult = new DynamicDiscoveryResult
        {
            TestClassType = typeof(T),
            TestClassArguments = dynamicTest.TestClassArguments,
            TestMethodArguments = dynamicTest.TestMethodArguments,
            TestMethod = dynamicTest.TestMethod,
            Attributes = dynamicTest.Attributes,
            CreatorFilePath = dynamicTest.CreatorFilePath,
            CreatorLineNumber = dynamicTest.CreatorLineNumber
        };

        // Queue the test for processing
        _pendingTests.Enqueue(new PendingDynamicTest
        {
            DiscoveryResult = discoveryResult,
            SourceContext = context,
            TestClassType = typeof(T)
        });

        // Process pending tests immediately
        await ProcessPendingDynamicTests();
    }

    [RequiresUnreferencedCode("Processing dynamic tests requires reflection which is not supported in native AOT scenarios.")]
    private async Task ProcessPendingDynamicTests()
    {
        var testsToProcess = new List<PendingDynamicTest>();

        while (_pendingTests.TryDequeue(out var pendingTest))
        {
            testsToProcess.Add(pendingTest);
        }

        if (testsToProcess.Count == 0 || _sessionId == null)
        {
            return;
        }

        // Create metadata for each dynamic test
        var testMetadataList = new List<TestMetadata>();

        foreach (var pendingTest in testsToProcess)
        {
            var result = pendingTest.DiscoveryResult;
            var metadata = await CreateMetadataFromDynamicDiscoveryResult(result);
            testMetadataList.Add(metadata);
        }

        // These are dynamic tests registered after discovery, so not in execution mode with a filter
        var buildingContext = new Building.TestBuildingContext(IsForExecution: false, Filter: null);
        var builtTests = await _testBuilderPipeline!.BuildTestsFromMetadataAsync(testMetadataList, buildingContext);

        foreach (var test in builtTests)
        {
            _dynamicTestQueue.Enqueue(test);
        }
    }

    [RequiresUnreferencedCode("Creating test variants requires reflection which is not supported in native AOT scenarios.")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access",
        Justification = "Dynamic test variants require reflection")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call",
        Justification = "Dynamic test variants require reflection")]
    [UnconditionalSuppressMessage("AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling",
        Justification = "Dynamic test variants require runtime compilation")]
    public async Task CreateTestVariant(
        TestContext currentContext,
        object?[]? arguments,
        Dictionary<string, object?>? properties,
        TUnit.Core.Enums.TestRelationship relationship,
        string? displayName)
    {
        var testDetails = currentContext.Metadata.TestDetails;
        var testClassType = testDetails.ClassType;
        var variantMethodArguments = arguments ?? testDetails.TestMethodArguments;

        var methodMetadata = testDetails.MethodMetadata;
        var parameterTypes = methodMetadata.Parameters.Select(p => p.Type).ToArray();
        var methodInfo = methodMetadata.Type.GetMethod(
            methodMetadata.Name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        if (methodInfo == null)
        {
            throw new InvalidOperationException($"Cannot create test variant: method '{methodMetadata.Name}' not found");
        }

        var genericAddDynamicTestMethod = typeof(TestRegistry)
            .GetMethod(nameof(CreateTestVariantInternal), BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(testClassType);

        if (genericAddDynamicTestMethod == null)
        {
            throw new InvalidOperationException("Failed to resolve CreateTestVariantInternal method");
        }

        await ((Task)genericAddDynamicTestMethod.Invoke(this,
            [currentContext, methodInfo, variantMethodArguments, testDetails.TestClassArguments, properties, relationship, displayName])!);
    }

    [RequiresUnreferencedCode("Creating test variants requires reflection which is not supported in native AOT scenarios.")]
    private async Task CreateTestVariantInternal<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(
        TestContext currentContext,
        MethodInfo methodInfo,
        object?[] variantMethodArguments,
        object?[] classArguments,
        Dictionary<string, object?>? properties,
        TUnit.Core.Enums.TestRelationship relationship,
        string? displayName) where T : class
    {
        var parameter = Expression.Parameter(typeof(T), "instance");
        var methodParameters = methodInfo.GetParameters();
        var argumentExpressions = new Expression[methodParameters.Length];

        for (int i = 0; i < methodParameters.Length; i++)
        {
            var argValue = i < variantMethodArguments.Length ? variantMethodArguments[i] : null;
            argumentExpressions[i] = Expression.Constant(argValue, methodParameters[i].ParameterType);
        }

        var methodCall = Expression.Call(parameter, methodInfo, argumentExpressions);

        Expression body;
        if (methodInfo.ReturnType == typeof(Task))
        {
            body = methodCall;
        }
        else if (methodInfo.ReturnType == typeof(void))
        {
            body = Expression.Block(methodCall, Expression.Constant(Task.CompletedTask));
        }
        else if (methodInfo.ReturnType.IsGenericType &&
                 methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            body = Expression.Convert(methodCall, typeof(Task));
        }
        else
        {
            body = Expression.Block(methodCall, Expression.Constant(Task.CompletedTask));
        }

        var lambda = Expression.Lambda<Func<T, Task>>(body, parameter);
        var attributes = new List<Attribute>(currentContext.Metadata.TestDetails.GetAllAttributes());

        var discoveryResult = new DynamicDiscoveryResult
        {
            TestClassType = typeof(T),
            TestClassArguments = classArguments,
            TestMethodArguments = variantMethodArguments,
            TestMethod = lambda,
            Attributes = attributes,
            CreatorFilePath = currentContext.Metadata.TestDetails.TestFilePath,
            CreatorLineNumber = currentContext.Metadata.TestDetails.TestLineNumber,
            ParentTestId = currentContext.Metadata.TestDetails.TestId,
            Relationship = relationship,
            Properties = properties,
            DisplayName = displayName
        };

        _pendingTests.Enqueue(new PendingDynamicTest
        {
            DiscoveryResult = discoveryResult,
            SourceContext = currentContext,
            TestClassType = typeof(T)
        });

        await ProcessPendingDynamicTests();
    }

    [RequiresUnreferencedCode("Dynamic test metadata creation requires reflection which is not supported in native AOT scenarios.")]
    private async Task<TestMetadata> CreateMetadataFromDynamicDiscoveryResult(DynamicDiscoveryResult result)
    {
        if (result.TestClassType == null || result.TestMethod == null)
        {
            throw new InvalidOperationException("Dynamic test discovery result must have a test class type and method");
        }

        // Extract method info from the expression
        MethodInfo? methodInfo = null;
        var lambdaExpression = result.TestMethod as LambdaExpression;
        if (lambdaExpression?.Body is MethodCallExpression methodCall)
        {
            methodInfo = methodCall.Method;
        }
        else if (lambdaExpression?.Body is UnaryExpression { Operand: MethodCallExpression unaryMethodCall })
        {
            methodInfo = unaryMethodCall.Method;
        }

        if (methodInfo == null)
        {
            throw new InvalidOperationException("Could not extract method info from dynamic test expression");
        }

        var testName = methodInfo.Name;

        return await Task.FromResult<TestMetadata>(new DynamicTestMetadata(result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
            TestMethodName = methodInfo.Name,
            Dependencies = result.Attributes.OfType<DependsOnAttribute>()
                .Select(x => x.ToTestDependency())
                .ToArray(),
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = [],
            InstanceFactory = CreateRuntimeInstanceFactory(result.TestClassType, result.TestClassArguments)!,
            TestInvoker = CreateRuntimeTestInvoker(result),
            FilePath = result.CreatorFilePath ?? "Unknown",
            LineNumber = result.CreatorLineNumber ?? 0,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(result.TestClassType, methodInfo),
            GenericTypeInfo = null,
            GenericMethodInfo = null,
            GenericMethodTypeArguments = null,
            AttributeFactory = () => GetAttributesOptimized(result.Attributes),
            PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(result.TestClassType)
        });
    }

    [RequiresUnreferencedCode("Dynamic test instance creation requires Activator.CreateInstance which is not supported in native AOT scenarios.")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call",
        Justification = "Dynamic tests require reflection")]
    private static Func<Type[], object?[], object>? CreateRuntimeInstanceFactory(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type testClass,
        object?[]? predefinedClassArgs)
    {
        var classArgs = predefinedClassArgs ?? [];

        return (typeArgs, args) =>
        {
            if (classArgs.Length == 0)
            {
                return Activator.CreateInstance(testClass)!;
            }
            return Activator.CreateInstance(testClass, classArgs)!;
        };
    }

    [RequiresUnreferencedCode("Dynamic test invocation requires LambdaExpression.Compile() which is not supported in native AOT scenarios.")]
    private static Func<object, object?[], Task> CreateRuntimeTestInvoker(DynamicDiscoveryResult result)
    {
        return async (instance, args) =>
        {
            if (result.TestMethod == null)
            {
                throw new InvalidOperationException("Dynamic test method expression is null");
            }

            var lambdaExpression = result.TestMethod as LambdaExpression;
            if (lambdaExpression == null)
            {
                throw new InvalidOperationException("Dynamic test method must be a lambda expression");
            }

            var compiledExpression = lambdaExpression.Compile();
            var testInstance = instance ?? throw new InvalidOperationException("Test instance is null");

            var invokeMethod = compiledExpression.GetType().GetMethod("Invoke")!;
            var invokeResult = invokeMethod.Invoke(compiledExpression, [testInstance]);

            if (invokeResult is Task task)
            {
                await task;
            }
            else if (invokeResult is ValueTask valueTask)
            {
                await valueTask;
            }
        };
    }

    private sealed class PendingDynamicTest
    {
        public required DynamicDiscoveryResult DiscoveryResult { get; init; }
        public required TestContext SourceContext { get; init; }
        public required Type TestClassType { get; init; }
    }

    /// <summary>
    /// Optimized method to convert attributes to array without LINQ allocations
    /// </summary>
    private static Attribute[] GetAttributesOptimized(ICollection<Attribute> attributes)
    {
        var result = new Attribute[attributes.Count];
        var index = 0;
        foreach (var attr in attributes)
        {
            result[index++] = attr;
        }
        return result;
    }
}
