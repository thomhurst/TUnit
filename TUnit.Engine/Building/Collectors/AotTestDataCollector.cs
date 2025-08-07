using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// AOT-compatible test data collector that uses source-generated test metadata.
/// Operates without reflection by leveraging pre-compiled test sources.
/// </summary>
internal sealed class AotTestDataCollector : ITestDataCollector, IStreamingTestDataCollector
{
    private readonly HashSet<Type>? _filterTypes;

    public AotTestDataCollector(HashSet<Type>? filterTypes)
    {
        _filterTypes = filterTypes;
    }
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Compatibility method - collects all from streaming
        var tests = new List<TestMetadata>();
        await foreach (var test in CollectTestsStreamingAsync(testSessionId, CancellationToken.None))
        {
            tests.Add(test);
        }
        return tests;
    }

    public async IAsyncEnumerable<TestMetadata> CollectTestsStreamingAsync(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stream from all test sources
        var testSources = Sources.TestSources
            .Where(kvp => _filterTypes == null || _filterTypes.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value);

        // Stream tests from each source
        foreach (var testSource in testSources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await foreach (var metadata in testSource.GetTestsAsync(testSessionId, cancellationToken))
            {
                yield return metadata;
            }
        }

        // Also stream dynamic tests
        await foreach (var metadata in CollectDynamicTestsStreaming(testSessionId, cancellationToken))
        {
            yield return metadata;
        }
    }

    private async IAsyncEnumerable<TestMetadata> CollectDynamicTestsStreaming(
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Sources.DynamicTestSources.Count == 0)
        {
            yield break;
        }

        // Stream from each dynamic test source
        foreach (var source in Sources.DynamicTestSources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            IEnumerable<DynamicTest> dynamicTests;
            TestMetadata? failedMetadata = null;
            
            try
            {
                dynamicTests = source.CollectDynamicTests(testSessionId);
            }
            catch (Exception ex)
            {
                // Create a failed test metadata for this dynamic test source
                failedMetadata = CreateFailedTestMetadataForDynamicSource(source, ex);
                dynamicTests = Enumerable.Empty<DynamicTest>();
            }

            if (failedMetadata != null)
            {
                yield return failedMetadata;
                continue;
            }

            foreach (var dynamicTest in dynamicTests)
            {
                // Convert each dynamic test to test metadata and stream
                await foreach (var metadata in ConvertDynamicTestToMetadataStreaming(dynamicTest, cancellationToken))
                {
                    yield return metadata;
                }
            }
        }
    }

    private async IAsyncEnumerable<TestMetadata> ConvertDynamicTestToMetadataStreaming(
        DynamicTest dynamicTest,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var discoveryResult in dynamicTest.GetTests())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (discoveryResult is DynamicDiscoveryResult { TestMethod: not null } dynamicResult)
            {
                var testMetadata = await CreateMetadataFromDynamicDiscoveryResult(dynamicResult);
                yield return testMetadata;
            }
        }
    }

    private Task<TestMetadata> CreateMetadataFromDynamicDiscoveryResult(DynamicDiscoveryResult result)
    {
        if (result.TestClassType == null || result.TestMethod == null)
        {
            throw new InvalidOperationException("Dynamic test discovery result must have a test class type and method");
        }

        // Extract method info from the expression
        System.Reflection.MethodInfo? methodInfo = null;
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

        return Task.FromResult<TestMetadata>(new AotDynamicTestMetadata(result)
        {
            TestName = testName,
#pragma warning disable IL2072
            TestClassType = result.TestClassType,
#pragma warning restore IL2072
            TestMethodName = methodInfo.Name,
            Dependencies = result.Attributes.OfType<DependsOnAttribute>().Select(a => a.ToTestDependency()).ToArray(),
            DataSources = [], // Dynamic tests don't use data sources in the same way
            ClassDataSources = [],
            PropertyDataSources = [],
            InstanceFactory = CreateAotDynamicInstanceFactory(result.TestClassType, result.TestClassArguments)!,
            TestInvoker = CreateAotDynamicTestInvoker(result),
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
#pragma warning disable IL2072
            PropertyInjections = PropertyInjectionService.DiscoverInjectableProperties(result.TestClassType)
#pragma warning restore IL2072
        });
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors()'",
        Justification = "AOT mode uses source-generated factories")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2067:Target parameter does not satisfy annotation requirements",
        Justification = "AOT mode uses source-generated factories")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2072:Target method return value does not have matching annotations",
        Justification = "AOT mode uses source-generated factories")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2055:Call to 'MakeGenericType' can not be statically analyzed",
        Justification = "Dynamic tests may use generic types")]
    [UnconditionalSuppressMessage("AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling",
        Justification = "Dynamic tests require dynamic code generation")]
    private static Func<Type[], object?[], object>? CreateAotDynamicInstanceFactory(Type testClass, object?[]? predefinedClassArgs)
    {
        // Check if we have predefined args to use as defaults
        var hasPredefinedArgs = predefinedClassArgs != null && predefinedClassArgs.Length > 0;

        return (typeArgs, args) =>
        {
            // Use provided args if available, otherwise fall back to predefined args
            var effectiveArgs = (args != null && args.Length > 0) ? args : (predefinedClassArgs ?? []);
            
            if (testClass.IsGenericTypeDefinition && typeArgs.Length > 0)
            {
                var closedType = testClass.MakeGenericType(typeArgs);
                if (effectiveArgs.Length == 0)
                {
                    return Activator.CreateInstance(closedType)!;
                }
                return Activator.CreateInstance(closedType, effectiveArgs)!;
            }

            if (effectiveArgs.Length == 0)
            {
                return Activator.CreateInstance(testClass)!;
            }
            return Activator.CreateInstance(testClass, effectiveArgs)!;
        };
    }

    private static Func<object, object?[], Task> CreateAotDynamicTestInvoker(DynamicDiscoveryResult result)
    {
        return async (instance, args) =>
        {
            try
            {
                if (result.TestMethod == null)
                {
                    throw new InvalidOperationException("Dynamic test method expression is null");
                }

                // Since we're in AOT mode, we need to handle this differently
                // The expression should already be compiled in source generation
                var lambdaExpression = result.TestMethod as LambdaExpression;
                if (lambdaExpression == null)
                {
                    throw new InvalidOperationException("Dynamic test method must be a lambda expression");
                }

                var compiledExpression = lambdaExpression.Compile();
                var testInstance = instance ?? throw new InvalidOperationException("Test instance is null");

                // The expression is already bound to the correct method with arguments
                // so we just need to invoke it with the instance
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
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(tie.InnerException ?? tie).Throw();
                throw;
            }
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "We won't instantiate this since it failed")]
    private static TestMetadata CreateFailedTestMetadataForDynamicSource(IDynamicTestSource source, Exception ex)
    {
        var testName = $"[DYNAMIC SOURCE FAILED] {source.GetType().Name}";

        return new FailedDynamicTestMetadata(ex)
        {
            TestName = testName,
            TestClassType = source.GetType(),
            TestMethodName = "CollectDynamicTests",
            MethodMetadata = CreateDummyMethodMetadata(source.GetType(), "CollectDynamicTests"),
            AttributeFactory = () => [],
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2067:Target parameter does not satisfy annotation requirements",
        Justification = "Dynamic test metadata creation")]
    [UnconditionalSuppressMessage("Trimming",
        "IL2072:Target method return value does not have matching annotations",
        Justification = "Dynamic test metadata creation")]
    private static MethodMetadata CreateDummyMethodMetadata(Type type, string methodName)
    {
        return new MethodMetadata
        {
            Name = methodName,
            Type = type,
            Class = new ClassMetadata
            {
                Name = type.Name,
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!),
                Namespace = type.Namespace ?? string.Empty,
                Assembly = new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? "Unknown"
                },
                Parameters = [],
                Properties = [],
                Parent = null
            },
            Parameters = [],
            GenericTypeCount = 0,
            ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName!),
            ReturnType = typeof(void),
            TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName!)
        };
    }

    private sealed class AotDynamicTestMetadata(DynamicDiscoveryResult dynamicResult) : TestMetadata, IDynamicTestMetadata
    {
        public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) =>
            {
                // For dynamic tests, we need to use the specific arguments from the dynamic result
                var modifiedContext = new ExecutableTestCreationContext
                {
                    TestId = context.TestId,
                    DisplayName = context.DisplayName,
                    Arguments = dynamicResult.TestMethodArguments ?? context.Arguments,
                    ClassArguments = dynamicResult.TestClassArguments ?? context.ClassArguments,
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

    private sealed class FailedDynamicTestMetadata(Exception exception) : TestMetadata
    {
        public override Func<ExecutableTestCreationContext, TestMetadata, AbstractExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) => new FailedExecutableTest(exception)
            {
                TestId = context.TestId,
                Metadata = metadata,
                Arguments = context.Arguments,
                ClassArguments = context.ClassArguments,
                Context = context.Context
            };
        }
    }
}
