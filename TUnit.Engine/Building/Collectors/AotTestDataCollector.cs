using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EnumerableAsyncProcessor.Extensions;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Engine.Building.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// AOT-compatible test data collector that uses source-generated test metadata.
/// Operates without reflection by leveraging pre-compiled test sources.
/// </summary>
internal sealed class AotTestDataCollector : ITestDataCollector
{
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "AOT implementation uses source-generated metadata, not reflection")]
    [UnconditionalSuppressMessage("AOT", "IL3051", Justification = "AOT implementation uses source-generated metadata, not dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic tests are optional and not used in AOT scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic tests are optional and not used in AOT scenarios")]
    #endif
    public Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId)
    {
        return CollectTestsAsync(testSessionId, filter: null);
    }

    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "AOT implementation uses source-generated metadata, not reflection")]
    [UnconditionalSuppressMessage("AOT", "IL3051", Justification = "AOT implementation uses source-generated metadata, not dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic tests are optional and not used in AOT scenarios")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Dynamic tests are optional and not used in AOT scenarios")]
    #endif
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync(string testSessionId, ITestExecutionFilter? filter)
    {
        // Extract hints from filter for pre-filtering test sources by type
        var filterHints = MetadataFilterMatcher.ExtractFilterHints(filter);

        // Get test sources, optionally pre-filtered by type
        IEnumerable<KeyValuePair<Type, ConcurrentQueue<ITestSource>>> testSourcesByType = Sources.TestSources;

        if (filterHints.HasHints)
        {
            // Pre-filter test sources by type based on filter hints
            var matchingSources = testSourcesByType.Where(kvp => filterHints.CouldTypeMatch(kvp.Key)).ToList();

            // Expand to include sources for dependency classes
            testSourcesByType = ExpandSourcesForDependencies(matchingSources, Sources.TestSources);
        }

        var testSourcesList = testSourcesByType.SelectMany(kvp => kvp.Value).ToList();

        // Try two-phase discovery for sources that support it (with specific filter hints)
        // This avoids creating full TestMetadata for tests that won't pass filtering
        IEnumerable<TestMetadata> standardTestMetadatas;

        if (filterHints.HasHints && testSourcesList.All(static s => s is ITestDescriptorSource))
        {
            // Two-phase discovery: enumerate descriptors, filter, then materialize only matching
            standardTestMetadatas = await CollectTestsWithTwoPhaseDiscoveryAsync(
                testSourcesList.Cast<ITestDescriptorSource>(),
                testSessionId,
                filterHints).ConfigureAwait(false);
        }
        else
        {
            // Fallback: Use traditional collection (for legacy sources or no filter hints)
            standardTestMetadatas = await CollectTestsTraditionalAsync(testSourcesList, testSessionId).ConfigureAwait(false);
        }

        // Dynamic tests are typically rare, collect sequentially
        var dynamicTestMetadatas = new List<TestMetadata>();
        await foreach (var metadata in CollectDynamicTestsStreaming(testSessionId))
        {
            dynamicTestMetadatas.Add(metadata);
        }

        return [..standardTestMetadatas, ..dynamicTestMetadatas];
    }

    /// <summary>
    /// Expands the pre-filtered sources to include sources for dependency classes.
    /// This ensures cross-class dependencies are included in two-phase discovery.
    /// </summary>
    private static IEnumerable<KeyValuePair<Type, ConcurrentQueue<ITestSource>>> ExpandSourcesForDependencies(
        List<KeyValuePair<Type, ConcurrentQueue<ITestSource>>> matchingSources,
        ConcurrentDictionary<Type, ConcurrentQueue<ITestSource>> allSources)
    {
        // Build index of all sources by class name for dependency lookup
        var sourcesByClassName = new Dictionary<string, KeyValuePair<Type, ConcurrentQueue<ITestSource>>>();
        foreach (var kvp in allSources)
        {
            sourcesByClassName[kvp.Key.Name] = kvp;
            // Also index without generic suffix (e.g., "MyClass`1" -> "MyClass")
            var backtickIndex = kvp.Key.Name.IndexOf('`');
            if (backtickIndex > 0)
            {
                sourcesByClassName[kvp.Key.Name.Substring(0, backtickIndex)] = kvp;
            }
        }

        // Collect all dependency class names from matching sources
        var dependencyClassNames = new HashSet<string>();
        foreach (var kvp in matchingSources)
        {
            foreach (var source in kvp.Value)
            {
                if (source is ITestDescriptorSource descriptorSource)
                {
                    foreach (var descriptor in descriptorSource.EnumerateTestDescriptors())
                    {
                        foreach (var dependency in descriptor.DependsOn)
                        {
                            // Parse dependency format: "ClassName:MethodName"
                            var separatorIndex = dependency.IndexOf(':');
                            if (separatorIndex > 0) // Cross-class dependency (not same-class ":MethodName")
                            {
                                var depClassName = dependency.Substring(0, separatorIndex);
                                dependencyClassNames.Add(depClassName);
                            }
                        }
                    }
                }
            }
        }

        // Build result set starting with matching sources
        var resultSet = new Dictionary<Type, KeyValuePair<Type, ConcurrentQueue<ITestSource>>>();
        foreach (var kvp in matchingSources)
        {
            resultSet[kvp.Key] = kvp;
        }

        // Expand dependencies transitively
        var queue = new Queue<string>(dependencyClassNames);
        var processedClasses = new HashSet<string>();

        while (queue.Count > 0)
        {
            var className = queue.Dequeue();
            if (!processedClasses.Add(className))
            {
                continue;
            }

            if (sourcesByClassName.TryGetValue(className, out var depSource) && !resultSet.ContainsKey(depSource.Key))
            {
                resultSet[depSource.Key] = depSource;

                // Check for transitive dependencies
                foreach (var source in depSource.Value)
                {
                    if (source is ITestDescriptorSource descriptorSource)
                    {
                        foreach (var descriptor in descriptorSource.EnumerateTestDescriptors())
                        {
                            foreach (var dependency in descriptor.DependsOn)
                            {
                                var separatorIndex = dependency.IndexOf(':');
                                if (separatorIndex > 0)
                                {
                                    var transDepClassName = dependency.Substring(0, separatorIndex);
                                    if (!processedClasses.Contains(transDepClassName))
                                    {
                                        queue.Enqueue(transDepClassName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return resultSet.Values;
    }

    /// <summary>
    /// Two-phase discovery: enumerate lightweight descriptors, apply filter hints, materialize only matching.
    /// This is more efficient when filters are present as it avoids creating full TestMetadata for non-matching tests.
    /// </summary>
    private async Task<IEnumerable<TestMetadata>> CollectTestsWithTwoPhaseDiscoveryAsync(
        IEnumerable<ITestDescriptorSource> descriptorSources,
        string testSessionId,
        FilterHints filterHints)
    {
        // Phase 1: Single-pass enumeration with filtering
        // - Index all descriptors for dependency resolution
        // - Immediately identify matching descriptors (no separate iteration)
        // - Track if any matching descriptor has dependencies
        var descriptorsByClassAndMethod = new Dictionary<(string ClassName, string MethodName), TestDescriptor>();
        var descriptorsByClass = new Dictionary<string, List<TestDescriptor>>();
        var matchingDescriptors = new List<TestDescriptor>();
        var hasDependencies = false;

        foreach (var source in descriptorSources)
        {
            foreach (var descriptor in source.EnumerateTestDescriptors())
            {
                // Index by class + method for specific dependency lookups
                var key = (descriptor.ClassName, descriptor.MethodName);
                descriptorsByClassAndMethod[key] = descriptor;

                // Index by class for class-level dependency lookups
                if (!descriptorsByClass.TryGetValue(descriptor.ClassName, out var classDescriptors))
                {
                    classDescriptors = [];
                    descriptorsByClass[descriptor.ClassName] = classDescriptors;
                }
                classDescriptors.Add(descriptor);

                // Filter during enumeration - no separate pass needed
                if (filterHints.CouldDescriptorMatch(descriptor))
                {
                    matchingDescriptors.Add(descriptor);
                    if (descriptor.DependsOn.Length > 0)
                    {
                        hasDependencies = true;
                    }
                }
            }
        }

        // Phase 2: Expand dependencies only if any matching descriptor has them
        HashSet<TestDescriptor>? expandedSet = null;
        if (hasDependencies)
        {
            expandedSet = new HashSet<TestDescriptor>(matchingDescriptors);
            var queue = new Queue<TestDescriptor>(matchingDescriptors);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var dependsOn = current.DependsOn;

                for (var i = 0; i < dependsOn.Length; i++)
                {
                    var dependency = dependsOn[i];

                    // Parse dependency format: "ClassName:MethodName"
                    var separatorIndex = dependency.IndexOf(':');
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    var depClassName = separatorIndex == 0
                        ? current.ClassName  // Same-class dependency
                        : dependency.Substring(0, separatorIndex);
                    var depMethodName = dependency.Substring(separatorIndex + 1);

                    if (depMethodName.Length > 0)
                    {
                        // Specific method dependency
                        if (descriptorsByClassAndMethod.TryGetValue((depClassName, depMethodName), out var depDescriptor))
                        {
                            if (expandedSet.Add(depDescriptor))
                            {
                                queue.Enqueue(depDescriptor);
                            }
                        }
                    }
                    else
                    {
                        // Class-level dependency: all tests in class
                        if (descriptorsByClass.TryGetValue(depClassName, out var classDescriptors))
                        {
                            foreach (var depDescriptor in classDescriptors)
                            {
                                if (expandedSet.Add(depDescriptor))
                                {
                                    queue.Enqueue(depDescriptor);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Phase 3: Materialize matching descriptors (including dependencies)
        var descriptorsToMaterialize = expandedSet ?? (IEnumerable<TestDescriptor>)matchingDescriptors;
        var results = new List<TestMetadata>();

        foreach (var descriptor in descriptorsToMaterialize)
        {
            await foreach (var metadata in descriptor.Materializer(testSessionId, CancellationToken.None).ConfigureAwait(false))
            {
                results.Add(metadata);
            }
        }

        return results;
    }

    /// <summary>
    /// Traditional collection: materialize all tests from sources.
    /// Used when filter hints are not available or sources don't support ITestDescriptorSource.
    /// </summary>
    private async Task<IEnumerable<TestMetadata>> CollectTestsTraditionalAsync(
        List<ITestSource> testSourcesList,
        string testSessionId)
    {
        // Use sequential processing for small test source sets to avoid task scheduling overhead
        if (testSourcesList.Count < Building.ParallelThresholds.MinItemsForParallel)
        {
            var results = new List<TestMetadata>();
            foreach (var testSource in testSourcesList)
            {
                await foreach (var metadata in testSource.GetTestsAsync(testSessionId))
                {
                    results.Add(metadata);
                }
            }
            return results;
        }
        else
        {
            return await testSourcesList
                .SelectManyAsync(testSource => testSource.GetTestsAsync(testSessionId))
                .ProcessInParallel();
        }
    }

    [RequiresUnreferencedCode("Dynamic test collection requires expression compilation and reflection")]
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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Dynamic test conversion requires expression compilation")]
    #endif
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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Dynamic test metadata creation requires expression extraction and reflection")]
    #endif
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

        return Task.FromResult<TestMetadata>(new DynamicTestMetadata(result)
        {
            TestName = testName,
            TestClassType = result.TestClassType,
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
            AttributeFactory = () => GetDynamicTestAttributes(result),
            PropertyInjections = PropertySourceRegistry.DiscoverInjectableProperties(result.TestClassType)
        });
    }

    private static Attribute[] GetDynamicTestAttributes(DynamicDiscoveryResult result)
    {
        if (result.TestClassType == null)
        {
            return result.Attributes.ToArray();
        }

        // Merge explicitly provided attributes with inherited class/assembly attributes
        // Order matches GetAllAttributes: method-level first (explicit), then class, then assembly
        var attributes = new List<Attribute>(result.Attributes);

        attributes.AddRange(result.TestClassType.GetCustomAttributes());
        attributes.AddRange(result.TestClassType.Assembly.GetCustomAttributes());

        return attributes.ToArray();
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can\'t be statically determined, or the type parameters to be used for generic arguments can\'t be statically determined.")]
    private static Func<Type[], object?[], object>? CreateAotDynamicInstanceFactory([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClass, object?[]? predefinedClassArgs)
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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Dynamic test invocation requires LambdaExpression.Compile")]
    #endif
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

                // Extract method info from the expression
                var lambdaExpression = result.TestMethod as LambdaExpression;
                if (lambdaExpression == null)
                {
                    throw new InvalidOperationException("Dynamic test method must be a lambda expression");
                }

                MethodInfo? methodInfo = null;
                if (lambdaExpression.Body is MethodCallExpression methodCall)
                {
                    methodInfo = methodCall.Method;
                }
                else if (lambdaExpression.Body is UnaryExpression { Operand: MethodCallExpression unaryMethodCall })
                {
                    methodInfo = unaryMethodCall.Method;
                }

                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Could not extract method info from dynamic test expression");
                }

                var testInstance = instance ?? throw new InvalidOperationException("Test instance is null");

                // Use the provided args from TestMethodArguments instead of the expression's placeholder values
                var invokeResult = methodInfo.Invoke(testInstance, args);

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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Failed metadata creation accesses Type.Name and assembly info")]
    #endif
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

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Dummy metadata creation accesses type and assembly information")]
    #endif
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
                TypeInfo = new ConcreteType(type),
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
            ReturnTypeInfo = new ConcreteType(typeof(void)),
            ReturnType = typeof(void),
            TypeInfo = new ConcreteType(type)
        };
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

    /// <summary>
    /// Enumerates lightweight test descriptors for fast filtering.
    /// For sources implementing ITestDescriptorSource, returns pre-computed descriptors.
    /// For legacy sources, creates descriptors with default filter hints.
    /// </summary>
    public IEnumerable<TestDescriptor> EnumerateDescriptors()
    {
        // Enumerate descriptors from all test sources
        foreach (var kvp in Sources.TestSources)
        {
            foreach (var testSource in kvp.Value)
            {
                // Check if the source implements ITestDescriptorSource for optimized enumeration
                if (testSource is ITestDescriptorSource descriptorSource)
                {
                    foreach (var descriptor in descriptorSource.EnumerateTestDescriptors())
                    {
                        yield return descriptor;
                    }
                }
                // For legacy sources without ITestDescriptorSource, we can't enumerate descriptors
                // without materializing - these will need to use the fallback path
            }
        }
    }

    /// <summary>
    /// Materializes full test metadata from filtered descriptors.
    /// Only called for tests that passed filtering, avoiding unnecessary materialization.
    /// </summary>
    public async IAsyncEnumerable<TestMetadata> MaterializeFromDescriptorsAsync(
        IEnumerable<TestDescriptor> descriptors,
        string testSessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var descriptor in descriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use the materializer delegate to create full TestMetadata
            await foreach (var metadata in descriptor.Materializer(testSessionId, cancellationToken).ConfigureAwait(false))
            {
                yield return metadata;
            }
        }
    }

    /// <summary>
    /// Gets test sources that don't implement ITestDescriptorSource.
    /// These sources require full materialization for discovery.
    /// </summary>
    public IEnumerable<ITestSource> GetLegacyTestSources()
    {
        foreach (var kvp in Sources.TestSources)
        {
            foreach (var testSource in kvp.Value)
            {
                if (testSource is not ITestDescriptorSource)
                {
                    yield return testSource;
                }
            }
        }
    }
}
