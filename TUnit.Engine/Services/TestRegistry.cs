using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building;
using TUnit.Engine.Scheduling;

namespace TUnit.Engine.Services;

/// <summary>
/// Service for registering and managing dynamically added tests during runtime execution.
/// </summary>
internal sealed class TestRegistry : ITestRegistry
{
    private readonly ConcurrentQueue<PendingDynamicTest> _pendingTests = new();
    private readonly TestBuilderPipeline? _testBuilderPipeline;
    private readonly HookOrchestratingTestExecutorAdapter _hookOrchestratingTestExecutorAdapter;
    private readonly CancellationToken _sessionCancellationToken;
    private readonly string? _sessionId;


    public TestRegistry(TestBuilderPipeline testBuilderPipeline,
        HookOrchestratingTestExecutorAdapter hookOrchestratingTestExecutorAdapter,
        string sessionId,
        CancellationToken sessionCancellationToken)
    {
        _testBuilderPipeline = testBuilderPipeline;
        _hookOrchestratingTestExecutorAdapter = hookOrchestratingTestExecutorAdapter;
        _sessionId = sessionId;
        _sessionCancellationToken = sessionCancellationToken;
    }
    public async Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] T>(TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Create a dynamic test discovery result
        var discoveryResult = new DynamicDiscoveryResult
        {
            TestClassType = typeof(T),
            TestClassArguments = dynamicTest.TestClassArguments,
            TestMethodArguments = dynamicTest.TestMethodArguments,
            TestMethod = dynamicTest.TestMethod,
            Attributes = dynamicTest.Attributes
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

        // Use the existing TestBuilderPipeline to build the tests
        // This ensures all the same logic is applied (repeat, retry, context creation, etc.)
        var builtTests = await _testBuilderPipeline!.BuildTestsFromMetadataAsync(testMetadataList);

        // Then execute each test through the single test executor
        foreach (var test in builtTests)
        {
            // The SingleTestExecutor will handle all execution-related message publishing
            await _hookOrchestratingTestExecutorAdapter.ExecuteTestAsync(test, _sessionCancellationToken);
        }
    }

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

        return await Task.FromResult<TestMetadata>(new RuntimeDynamicTestMetadata(result.TestClassType, methodInfo, result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
            TestMethodName = methodInfo.Name,
            IsSkipped = result.Attributes.OfType<SkipAttribute>().Any(),
            SkipReason = result.Attributes.OfType<SkipAttribute>().FirstOrDefault()?.Reason,
            TimeoutMs = (int?)result.Attributes.OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout.TotalMilliseconds,
            RetryCount = result.Attributes.OfType<RetryAttribute>().FirstOrDefault()?.Times ?? 0,
            RepeatCount = result.Attributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0,
            CanRunInParallel = !result.Attributes.OfType<NotInParallelAttribute>().Any(),
            Dependencies = result.Attributes.OfType<DependsOnAttribute>().Select(a => a.ToTestDependency()).ToArray(),
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = [],
            InstanceFactory = CreateRuntimeInstanceFactory(result.TestClassType, result.TestClassArguments)!,
            TestInvoker = CreateRuntimeTestInvoker(result),
            ParameterCount = result.TestMethodArguments?.Length ?? 0,
            ParameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
            TestMethodParameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name).ToArray(),
            FilePath = null,
            LineNumber = null,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(result.TestClassType, methodInfo),
            GenericTypeInfo = null,
            GenericMethodInfo = null,
            GenericMethodTypeArguments = null,
            AttributeFactory = () => result.Attributes.ToArray(),
            PropertyInjections = PropertyInjector.DiscoverInjectableProperties(result.TestClassType)
        });
    }

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
            var invokeResult = invokeMethod.Invoke(compiledExpression, new[] { testInstance });

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


    private sealed class RuntimeDynamicTestMetadata : TestMetadata, IDynamicTestMetadata
    {
        private readonly DynamicDiscoveryResult _dynamicResult;
        private readonly Type _testClass;
        private readonly MethodInfo _testMethod;

        public RuntimeDynamicTestMetadata(Type testClass, MethodInfo testMethod, DynamicDiscoveryResult dynamicResult)
        {
            _testClass = testClass;
            _testMethod = testMethod;
            _dynamicResult = dynamicResult;
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) =>
            {
                // For dynamic tests, we need to use the specific arguments from the dynamic result
                var modifiedContext = new ExecutableTestCreationContext
                {
                    TestId = context.TestId,
                    DisplayName = context.DisplayName,
                    Arguments = _dynamicResult.TestMethodArguments ?? context.Arguments,
                    ClassArguments = _dynamicResult.TestClassArguments ?? context.ClassArguments,
                    Context = context.Context
                };

                // Create instance and test invoker for the dynamic test
                Func<TestContext, Task<object>> createInstance = (TestContext testContext) =>
                {
                    var instance = metadata.InstanceFactory(Type.EmptyTypes, modifiedContext.ClassArguments);

                    // Handle property injections
                    foreach (var propertyInjection in metadata.PropertyInjections)
                    {
                        var value = propertyInjection.ValueFactory();
                        propertyInjection.Setter(instance, value);
                    }

                    return Task.FromResult(instance);
                };

                var invokeTest = metadata.TestInvoker ?? throw new InvalidOperationException("Test invoker is null");

                return new ExecutableTest(createInstance,
                    async (instance, args, context, ct) => await invokeTest(instance, args))
                {
                    TestId = modifiedContext.TestId,
                    Metadata = metadata,
                    Arguments = modifiedContext.Arguments,
                    ClassArguments = modifiedContext.ClassArguments,
                    Context = modifiedContext.Context
                };
            };
        }
    }
}
