using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Helpers;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// AOT-compatible test data collector that uses source-generated test metadata.
/// Operates without reflection by leveraging pre-compiled test sources.
/// </summary>
internal sealed class AotTestDataCollector : ITestDataCollector
{
    private readonly HashSet<Type>? _filterTypes;

    public AotTestDataCollector(HashSet<Type>? filterTypes)
    {
        _filterTypes = filterTypes;
    }
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        // Get all test sources as a list to enable indexed parallel processing
        var testSourcesList = Sources.TestSources
            .Where(kvp => _filterTypes == null || _filterTypes.Contains(kvp.Key))
            .SelectMany(kvp => kvp.Value)
            .ToList();

        if (testSourcesList.Count == 0)
        {
            return [];
        }

        // Use indexed collection to maintain order and prevent race conditions
        var resultsByIndex = new ConcurrentDictionary<int, IEnumerable<TestMetadata>>();

        // Use true parallel processing with optimal concurrency
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(testSourcesList.Select((source, index) => new { source, index }),
                parallelOptions, item =>
                {
                    var index = item.index;
                    var testSource = item.source;

                    try
                    {
                        // Run async method synchronously since we're already on thread pool
                        var tests = testSource.GetTestsAsync(testSessionId).GetAwaiter().GetResult();
                        resultsByIndex[index] = tests;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to collect tests from source {testSource.GetType().Name}: {ex.Message}", ex);
                    }
                });
        });

        // Reassemble results in original order
        var allTests = new List<TestMetadata>();
        for (var i = 0; i < testSourcesList.Count; i++)
        {
            if (resultsByIndex.TryGetValue(i, out var tests))
            {
                allTests.AddRange(tests);
            }
        }

        // Also collect dynamic tests from registered dynamic test sources
        var dynamicTests = await CollectDynamicTests(testSessionId);
        allTests.AddRange(dynamicTests);

        if (allTests.Count == 0)
        {
            // No generated tests found
            return [
            ];
        }

        return allTests;
    }

    private async Task<List<TestMetadata>> CollectDynamicTests(string testSessionId)
    {
        var dynamicTestMetadata = new List<TestMetadata>();

        if (Sources.DynamicTestSources.Count == 0)
        {
            return dynamicTestMetadata;
        }

        // Convert dynamic test sources to list for parallel processing
        var dynamicSourcesList = Sources.DynamicTestSources.ToList();

        // Use indexed collection to maintain order
        var resultsByIndex = new ConcurrentDictionary<int, List<TestMetadata>>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Task.Run(() =>
        {
            Parallel.ForEach(dynamicSourcesList.Select((source, index) => new { source, index }),
                parallelOptions, item =>
                {
                    var index = item.index;
                    var source = item.source;
                    var testsForSource = new List<TestMetadata>();

                    try
                    {
                        var dynamicTests = source.CollectDynamicTests(testSessionId);
                        foreach (var dynamicTest in dynamicTests)
                        {
                            // Convert each dynamic test to test metadata
                            var metadataList = ConvertDynamicTestToMetadata(dynamicTest).GetAwaiter().GetResult();
                            testsForSource.AddRange(metadataList);
                        }
                        resultsByIndex[index] = testsForSource;
                    }
                    catch (Exception ex)
                    {
                        // Create a failed test metadata for this dynamic test source
                        var failedTest = CreateFailedTestMetadataForDynamicSource(source, ex);
                        resultsByIndex[index] = [failedTest];
                    }
                });
        });

        // Reassemble results in original order
        for (var i = 0; i < dynamicSourcesList.Count; i++)
        {
            if (resultsByIndex.TryGetValue(i, out var tests))
            {
                dynamicTestMetadata.AddRange(tests);
            }
        }

        return dynamicTestMetadata;
    }

    private async Task<List<TestMetadata>> ConvertDynamicTestToMetadata(DynamicTest dynamicTest)
    {
        var testMetadataList = new List<TestMetadata>();

        foreach (var discoveryResult in dynamicTest.GetTests())
        {
            if (discoveryResult is DynamicDiscoveryResult { TestMethod: not null } dynamicResult)
            {
                var testMetadata = await CreateMetadataFromDynamicDiscoveryResult(dynamicResult);
                testMetadataList.Add(testMetadata);
            }
        }

        return testMetadataList;
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

        return Task.FromResult<TestMetadata>(new AotDynamicTestMetadata(result.TestClassType, methodInfo, result)
        {
            TestName = testName,
#pragma warning disable IL2072
            TestClassType = result.TestClassType,
#pragma warning restore IL2072
            TestMethodName = methodInfo.Name,
            Categories = result.Attributes.OfType<CategoryAttribute>().Select(a => a.Category).ToArray(),
            IsSkipped = result.Attributes.OfType<SkipAttribute>().Any(),
            SkipReason = result.Attributes.OfType<SkipAttribute>().FirstOrDefault()?.Reason,
            TimeoutMs = (int?)result.Attributes.OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout.TotalMilliseconds,
            RetryCount = result.Attributes.OfType<RetryAttribute>().FirstOrDefault()?.Times ?? 0,
            RepeatCount = result.Attributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 1,
            CanRunInParallel = !result.Attributes.OfType<NotInParallelAttribute>().Any(),
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
            MethodMetadata = MetadataBuilder.CreateMethodMetadata(result.TestClassType, methodInfo),
            GenericTypeInfo = null,
            GenericMethodInfo = null,
            GenericMethodTypeArguments = null,
            AttributeFactory = () => result.Attributes.ToArray(),
#pragma warning disable IL2072
            PropertyInjections = PropertyInjector.DiscoverInjectableProperties(result.TestClassType)
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
        // For dynamic tests, we always use the predefined args (or empty array if null)
        var classArgs = predefinedClassArgs ?? [];
        
        return (typeArgs, args) =>
        {
            // Always use the predefined class args, ignoring the args parameter
            if (testClass.IsGenericTypeDefinition && typeArgs.Length > 0)
            {
                var closedType = testClass.MakeGenericType(typeArgs);
                if (classArgs.Length == 0)
                {
                    return Activator.CreateInstance(closedType)!;
                }
                return Activator.CreateInstance(closedType, classArgs)!;
            }
            
            if (classArgs.Length == 0)
            {
                return Activator.CreateInstance(testClass)!;
            }
            return Activator.CreateInstance(testClass, classArgs)!;
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

    private static TestMetadata CreateFailedTestMetadataForDynamicSource(IDynamicTestSource source, Exception ex)
    {
        var testName = $"[DYNAMIC SOURCE FAILED] {source.GetType().Name}";
        var displayName = $"{testName} - {ex.Message}";

        return new FailedDynamicTestMetadata(ex, displayName)
        {
            TestName = testName,
#pragma warning disable IL2072
            TestClassType = source.GetType(),
#pragma warning restore IL2072
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

    private sealed class AotDynamicTestMetadata : TestMetadata, IDynamicTestMetadata
    {
        private readonly DynamicDiscoveryResult _dynamicResult;
        private readonly Type _testClass;
        private readonly System.Reflection.MethodInfo _testMethod;

        public AotDynamicTestMetadata(Type testClass, System.Reflection.MethodInfo testMethod, DynamicDiscoveryResult dynamicResult)
        {
            _testClass = testClass;
            _testMethod = testMethod;
            _dynamicResult = dynamicResult;
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
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
                
                return new UnifiedExecutableTest(createInstance, 
                    async (instance, args, context, ct) => await invokeTest(instance, args))
                {
                    TestId = modifiedContext.TestId,
                    DisplayName = modifiedContext.DisplayName,
                    Metadata = metadata,
                    Arguments = modifiedContext.Arguments,
                    ClassArguments = modifiedContext.ClassArguments,
                    Context = modifiedContext.Context
                };
            };
        }
    }

    private sealed class FailedDynamicTestMetadata : TestMetadata
    {
        private readonly Exception _exception;
        private readonly string _displayName;

        public FailedDynamicTestMetadata(Exception exception, string displayName)
        {
            _exception = exception;
            _displayName = displayName;
        }

        public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
        {
            get => (context, metadata) => new FailedExecutableTest(_exception)
            {
                TestId = context.TestId,
                DisplayName = context.DisplayName,
                Metadata = metadata,
                Arguments = context.Arguments,
                ClassArguments = context.ClassArguments,
                Context = context.Context
            };
        }
    }
}