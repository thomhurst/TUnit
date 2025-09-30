using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Extensions;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// AOT-compatible test data collector that uses source-generated test metadata.
/// Operates without reflection by leveraging pre-compiled test sources.
/// </summary>
internal sealed class AotTestDataCollector : ITestDataCollector
{
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Stream from all test sources
        var testSources = Sources.TestSources
            .SelectMany(kvp => kvp.Value);

        var standardTestMetadatas = await testSources
            .SelectManyAsync(testSource => testSource.GetTestsAsync(testSessionId))
            .ProcessInParallel();

        var dynamicTestMetadatas = await CollectDynamicTestsStreaming(testSessionId)
            .ProcessInParallel();

        return [..standardTestMetadatas, ..dynamicTestMetadatas];
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

            IEnumerable<AbstractDynamicTest> dynamicTests;
            TestMetadata? failedMetadata = null;

            try
            {
                dynamicTests = source.CollectDynamicTests(testSessionId);
            }
            catch (Exception ex)
            {
                // Create a failed test metadata for this dynamic test source
                failedMetadata = CreateFailedTestMetadataForDynamicSource(source, ex);
                dynamicTests = [];
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

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Dynamic tests are opt-in and users are warned via RequiresDynamicCode on the method they call")]
    private async IAsyncEnumerable<TestMetadata> ConvertDynamicTestToMetadataStreaming(
        AbstractDynamicTest abstractDynamicTest,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var discoveryResult in abstractDynamicTest.GetTests())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (discoveryResult is DynamicDiscoveryResult { TestMethod: not null } dynamicResult)
            {
                var testMetadata = await CreateMetadataFromDynamicDiscoveryResult(dynamicResult);
                yield return testMetadata;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Dynamic tests require runtime compilation of lambda expressions and are not supported in native AOT scenarios.")]
    private Task<TestMetadata> CreateMetadataFromDynamicDiscoveryResult(DynamicDiscoveryResult result)
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
            FilePath = result.CreatorFilePath ?? "Unknown",
            LineNumber = result.CreatorLineNumber ?? 0,
            MethodMetadata = ReflectionMetadataBuilder.CreateMethodMetadata(result.TestClassType, methodInfo),
            GenericTypeInfo = null,
            GenericMethodInfo = null,
            GenericMethodTypeArguments = null,
            AttributeFactory = () => result.Attributes.ToArray(),
#pragma warning disable IL2072
            PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(result.TestClassType)
#pragma warning restore IL2072
        });
    }

    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Dynamic test instance creation requires Activator.CreateInstance and MakeGenericType which are not supported in native AOT scenarios.")]
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
        var hasPredefinedArgs = predefinedClassArgs is { Length: > 0 };

        return (typeArgs, args) =>
        {
            // Use provided args if available, otherwise fall back to predefined args
            var effectiveArgs = args is { Length: > 0 } ? args : predefinedClassArgs ?? [];

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

    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Dynamic test invocation requires LambdaExpression.Compile() which is not supported in native AOT scenarios.")]
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
                var invokeResult = invokeMethod.Invoke(compiledExpression, [testInstance]);

                if (invokeResult is Task task)
                {
                    await task;
                }
                else if (invokeResult is ValueTask valueTask)
                {
                    await valueTask;
                }
            }
            catch (TargetInvocationException tie)
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
            FilePath = "Unknown",
            LineNumber = 0,
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
                var createInstance = async (TestContext testContext) =>
                {
                    object instance;
                    
                    // Check if there's a ClassConstructor to use
                    if (testContext.ClassConstructor != null)
                    {
                        var testBuilderContext = TestBuilderContext.FromTestContext(testContext, null);
                        var classConstructorMetadata = new ClassConstructorMetadata
                        {
                            TestSessionId = "", // Dynamic tests don't have session IDs
                            TestBuilderContext = testBuilderContext
                        };
                        
                        instance = await testContext.ClassConstructor.Create(metadata.TestClassType, classConstructorMetadata);
                    }
                    else
                    {
                        instance = metadata.InstanceFactory(Type.EmptyTypes, modifiedContext.ClassArguments);
                    }

                    // Handle property injections
                    foreach (var propertyInjection in metadata.PropertyInjections)
                    {
                        var value = propertyInjection.ValueFactory();
                        propertyInjection.Setter(instance, value);
                    }

                    return instance;
                };

                var invokeTest = metadata.TestInvoker ?? throw new InvalidOperationException("Test invoker is null");

                return new ExecutableTest(createInstance,
                    async (instance, args, context, ct) =>
                    {
                        await invokeTest(instance, args);
                    })
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
