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
using TUnit.Engine.Helpers;
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
        var filterHints = MetadataFilterMatcher.ExtractFilterHints(filter);

        var allResults = new List<TestMetadata>();

        // Phase A: Collect from TestEntry sources (new fast path)
        if (!Sources.TestEntries.IsEmpty)
        {
            var entryResults = CollectTestsFromTestEntries(testSessionId, filterHints);
            allResults.AddRange(entryResults);
        }

        // Phase B: Collect from ITestSource registered sources (generic/inherited tests)
        if (!Sources.TestSources.IsEmpty)
        {
            IEnumerable<TestMetadata> standardTestMetadatas;

            if (filterHints.HasHints && Sources.TestSources.All(static kvp => kvp.Value.All(static s => s is ITestDescriptorSource)))
            {
                standardTestMetadatas = CollectTestsWithTwoPhaseDiscovery(
                    Sources.TestSources,
                    testSessionId,
                    filterHints);
            }
            else
            {
                IEnumerable<KeyValuePair<Type, ConcurrentQueue<ITestSource>>> testSourcesByType = Sources.TestSources;

                if (filterHints.HasHints)
                {
                    testSourcesByType = testSourcesByType.Where(kvp => filterHints.CouldTypeMatch(kvp.Key));
                }

                var testSourcesList = testSourcesByType.SelectMany(kvp => kvp.Value).ToList();
                standardTestMetadatas = CollectTests(testSourcesList, testSessionId);
            }

            allResults.AddRange(standardTestMetadatas);
        }

        // Phase C: Dynamic tests (typically rare)
        await foreach (var metadata in CollectDynamicTestsStreaming(testSessionId))
        {
            allResults.Add(metadata);
        }

        return allResults;
    }

    /// <summary>
    /// Collects tests from TestEntry sources (the new fast path).
    /// Uses TestEntryFilterData for pure-data filtering, then materializes only matching entries.
    /// </summary>
    #if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Materialization uses source-generated metadata")]
    #endif
    private IEnumerable<TestMetadata> CollectTestsFromTestEntries(
        string testSessionId,
        FilterHints filterHints)
    {
        // Phase 1: Filter using pure data (no JIT of test-specific methods)
        var totalEntries = Sources.TestEntries.Sum(static kvp => kvp.Value.Count);
        var matching = new List<(ITestEntrySource Source, int Index)>(totalEntries);
        var hasDependencies = false;

        foreach (var kvp in Sources.TestEntries)
        {
            var classType = kvp.Key;
            var source = kvp.Value;
            var typeMatches = !filterHints.HasHints || filterHints.CouldTypeMatch(classType);

            for (var i = 0; i < source.Count; i++)
            {
                var filterData = source.GetFilterData(i);

                if (typeMatches && (!filterHints.HasHints || filterHints.CouldMatch(filterData.ClassName, filterData.MethodName)))
                {
                    matching.Add((source, i));
                    if (filterData.DependsOn.Length > 0)
                    {
                        hasDependencies = true;
                    }
                }
            }
        }

        // Phase 2: Expand dependencies via BFS (only if needed)
        HashSet<(ITestEntrySource, int)>? expandedSet = null;
        if (hasDependencies)
        {
            var byClassAndMethod = new Dictionary<(string, string), (ITestEntrySource Source, int Index)>();
            var byClass = new Dictionary<string, List<(ITestEntrySource Source, int Index)>>();

            foreach (var kvp in Sources.TestEntries)
            {
                var source = kvp.Value;
                for (var i = 0; i < source.Count; i++)
                {
                    var fd = source.GetFilterData(i);
                    var pair = (source, i);
                    byClassAndMethod[(fd.ClassName, fd.MethodName)] = pair;

                    if (!byClass.TryGetValue(fd.ClassName, out var list))
                    {
                        list = [];
                        byClass[fd.ClassName] = list;
                    }
                    list.Add(pair);
                }
            }

            expandedSet = new HashSet<(ITestEntrySource, int)>(matching);
            var queue = new Queue<(ITestEntrySource Source, int Index)>(matching);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var fd = current.Source.GetFilterData(current.Index);

                foreach (var dep in fd.DependsOn)
                {
                    var sep = dep.IndexOf(':');
                    if (sep < 0) continue;

                    var depClass = sep == 0 ? fd.ClassName : dep[..sep];
                    var depMethod = dep[(sep + 1)..];

                    if (depMethod.Length > 0)
                    {
                        if (byClassAndMethod.TryGetValue((depClass, depMethod), out var depEntry)
                            && expandedSet.Add(depEntry))
                            queue.Enqueue(depEntry);
                    }
                    else
                    {
                        if (byClass.TryGetValue(depClass, out var classEntries))
                        {
                            foreach (var depEntry in classEntries)
                            {
                                if (expandedSet.Add(depEntry))
                                    queue.Enqueue(depEntry);
                            }
                        }
                    }
                }
            }
        }

        // Phase 3: Materialize
        var toMaterialize = expandedSet ?? (IEnumerable<(ITestEntrySource Source, int Index)>)matching;
        var results = new List<TestMetadata>();

        foreach (var (source, index) in toMaterialize)
        {
            var materialized = source.Materialize(index, testSessionId);
            for (var i = 0; i < materialized.Count; i++)
            {
                results.Add(materialized[i]);
            }
        }

        return results;
    }

    /// <summary>
    /// Two-phase discovery with single-pass filtering and dependency resolution.
    /// Accepts ALL test sources (with their associated types) and performs type-level
    /// and descriptor-level filtering in a single enumeration pass, while indexing
    /// all descriptors for dependency resolution.
    ///
    /// This avoids the previous double-enumeration where ExpandSourcesForDependencies
    /// enumerated descriptors to find dependencies, and then this method enumerated
    /// them again for filtering and materialization.
    /// </summary>
    private IEnumerable<TestMetadata> CollectTestsWithTwoPhaseDiscovery(
        IEnumerable<KeyValuePair<Type, ConcurrentQueue<ITestSource>>> allSourcesByType,
        string testSessionId,
        FilterHints filterHints)
    {
        // Phase 1: Single-pass enumeration over ALL sources with combined filtering
        // - Index ALL descriptors (from all types) for dependency resolution
        // - Apply type-level filter (assembly, namespace, class) per source group
        // - Apply descriptor-level filter (class name, method name) per descriptor
        // - Only descriptors passing BOTH filters are added to matchingDescriptors
        // - Track if any matching descriptor has dependencies
        var descriptorsByClassAndMethod = new Dictionary<(string ClassName, string MethodName), TestDescriptor>();
        var descriptorsByClass = new Dictionary<string, List<TestDescriptor>>();
        var matchingDescriptors = new List<TestDescriptor>();
        var hasDependencies = false;

        foreach (var kvp in allSourcesByType)
        {
            // Check type-level filter once per source group (covers assembly, namespace, class name)
            var typeMatches = filterHints.CouldTypeMatch(kvp.Key);

            foreach (var source in kvp.Value)
            {
                var descriptorSource = (ITestDescriptorSource)source;

                foreach (var descriptor in descriptorSource.EnumerateTestDescriptors())
                {
                    // Always index for dependency resolution regardless of filter match
                    var key = (descriptor.ClassName, descriptor.MethodName);
                    descriptorsByClassAndMethod[key] = descriptor;

                    if (!descriptorsByClass.TryGetValue(descriptor.ClassName, out var classDescriptors))
                    {
                        classDescriptors = [];
                        descriptorsByClass[descriptor.ClassName] = classDescriptors;
                    }
                    classDescriptors.Add(descriptor);

                    // Only add to matching set if both type-level and descriptor-level filters pass
                    if (typeMatches && filterHints.CouldDescriptorMatch(descriptor))
                    {
                        matchingDescriptors.Add(descriptor);
                        if (descriptor.DependsOn.Length > 0)
                        {
                            hasDependencies = true;
                        }
                    }
                }
            }
        }

        // Phase 2: Expand dependencies only if any matching descriptor has them.
        // Because all descriptors are indexed (not just filtered ones), cross-class
        // and transitive dependencies are resolved correctly even when the dependency
        // target was filtered out by type/descriptor hints.
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
            var materialized = descriptor.Materializer(testSessionId);
            for (var i = 0; i < materialized.Count; i++)
            {
                results.Add(materialized[i]);
            }
        }

        return results;
    }

    private List<TestMetadata> CollectTests(
        List<ITestSource> testSourcesList,
        string testSessionId)
    {
        var batches = new IReadOnlyList<TestMetadata>[testSourcesList.Count];

        Parallel.For(0, testSourcesList.Count, i =>
        {
            batches[i] = testSourcesList[i].GetTests(testSessionId);
        });

        var totalCount = 0;
        for (var i = 0; i < batches.Length; i++)
        {
            totalCount += batches[i].Count;
        }

        var combined = new List<TestMetadata>(totalCount);
        for (var i = 0; i < batches.Length; i++)
        {
            var batch = batches[i];
            for (var j = 0; j < batch.Count; j++)
            {
                combined.Add(batch[j]);
            }
        }
        return combined;
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

        var methodInfo = ExpressionHelper.ExtractMethodInfo(result.TestMethod);

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
                var methodInfo = ExpressionHelper.ExtractMethodInfo(result.TestMethod);

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
    public IEnumerable<TestMetadata> MaterializeFromDescriptors(
        IEnumerable<TestDescriptor> descriptors,
        string testSessionId)
    {
        foreach (var descriptor in descriptors)
        {
            var materialized = descriptor.Materializer(testSessionId);
            for (var i = 0; i < materialized.Count; i++)
            {
                yield return materialized[i];
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
