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
        // Extract hints from filter for pre-filtering test sources by type
        var filterHints = MetadataFilterMatcher.ExtractFilterHints(filter);

        var allResults = new List<TestMetadata>();

        // Phase A: Collect from data-table registered sources (new fast path)
        // These entries are pure data — no JIT needed for filtering.
        // Only matching entries trigger materialization (deferred JIT).
        if (!Sources.TableEntries.IsEmpty)
        {
            var tableResults = CollectTestsFromTableEntries(testSessionId, filterHints);
            allResults.AddRange(tableResults);
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
                standardTestMetadatas = CollectTestsTraditional(testSourcesList, testSessionId);
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
    /// Collects tests from data-table registered sources (the new fast path).
    /// Iterates over TestRegistrationEntry[] arrays (pure data, no JIT) for filtering,
    /// then calls materializers only for matching entries (deferred JIT, 1 per class).
    /// </summary>
    private IEnumerable<TestMetadata> CollectTestsFromTableEntries(
        string testSessionId,
        FilterHints filterHints)
    {
        // Phase 1: Filter entries. Dependency indices are lazy-initialized only if needed.
        var matchingEntries = new List<(Type ClassType, TestRegistrationEntry Entry)>();
        Dictionary<(string ClassName, string MethodName), (Type ClassType, TestRegistrationEntry Entry)>? entriesByClassAndMethod = null;
        Dictionary<string, List<(Type ClassType, TestRegistrationEntry Entry)>>? entriesByClass = null;
        var hasDependencies = false;

        foreach (var kvp in Sources.TableEntries)
        {
            var classType = kvp.Key;
            var entries = kvp.Value;
            var typeMatches = !filterHints.HasHints || filterHints.CouldTypeMatch(classType);

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                if (typeMatches && CouldEntryMatch(entry, filterHints))
                {
                    matchingEntries.Add((classType, entry));
                    if (entry.DependsOn.Length > 0)
                    {
                        hasDependencies = true;
                    }
                }
            }
        }

        // Phase 2: Expand dependencies via BFS (only if any matching entry has them)
        HashSet<(Type, TestRegistrationEntry)>? expandedSet = null;
        if (hasDependencies)
        {
            // Build dependency indices lazily — only needed when dependencies exist
            entriesByClassAndMethod = new(capacity: Sources.TableEntries.Sum(kvp => kvp.Value.Length));
            entriesByClass = new(capacity: Sources.TableEntries.Count);

            foreach (var kvp in Sources.TableEntries)
            {
                var classType = kvp.Key;
                var entries = kvp.Value;
                for (var i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    var pair = (classType, entry);
                    entriesByClassAndMethod[(entry.ClassName, entry.MethodName)] = pair;

                    if (!entriesByClass.TryGetValue(entry.ClassName, out var classEntries))
                    {
                        classEntries = [];
                        entriesByClass[entry.ClassName] = classEntries;
                    }
                    classEntries.Add(pair);
                }
            }

            expandedSet = new HashSet<(Type, TestRegistrationEntry)>(matchingEntries);
            var queue = new Queue<(Type ClassType, TestRegistrationEntry Entry)>(matchingEntries);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var dependsOn = current.Entry.DependsOn;

                for (var i = 0; i < dependsOn.Length; i++)
                {
                    var dependency = dependsOn[i];
                    var separatorIndex = dependency.IndexOf(':');
                    if (separatorIndex < 0) continue;

                    var depClassName = separatorIndex == 0
                        ? current.Entry.ClassName
                        : dependency.Substring(0, separatorIndex);
                    var depMethodName = dependency.Substring(separatorIndex + 1);

                    if (depMethodName.Length > 0)
                    {
                        if (entriesByClassAndMethod.TryGetValue((depClassName, depMethodName), out var depEntry))
                        {
                            if (expandedSet.Add(depEntry))
                                queue.Enqueue(depEntry);
                        }
                    }
                    else
                    {
                        if (entriesByClass.TryGetValue(depClassName, out var classEntries))
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

        // Phase 3: Materialize matching entries
        var entriesToMaterialize = expandedSet ?? (IEnumerable<(Type ClassType, TestRegistrationEntry Entry)>)matchingEntries;
        var results = new List<TestMetadata>();

        foreach (var (classType, entry) in entriesToMaterialize)
        {
            if (Sources.TableMaterializers.TryGetValue(classType, out var materializer))
            {
                var materialized = materializer(entry.MethodIndex, testSessionId);
                for (var i = 0; i < materialized.Count; i++)
                {
                    results.Add(materialized[i]);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Checks if a TestRegistrationEntry could match the given filter hints.
    /// Pure data comparison — no JIT of any test-specific methods.
    /// </summary>
    private static bool CouldEntryMatch(TestRegistrationEntry entry, FilterHints filterHints)
    {
        if (!filterHints.HasHints)
        {
            return true;
        }

        return filterHints.CouldEntryMatch(entry);
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

    /// <summary>
    /// Traditional collection: materialize all tests from sources.
    /// Used when filter hints are not available or sources don't support ITestDescriptorSource.
    /// </summary>
    private IEnumerable<TestMetadata> CollectTestsTraditional(
        List<ITestSource> testSourcesList,
        string testSessionId)
    {
        var results = new List<TestMetadata>();
        foreach (var testSource in testSourcesList)
        {
            var tests = testSource.GetTests(testSessionId);
            for (var i = 0; i < tests.Count; i++)
            {
                results.Add(tests[i]);
            }
        }
        return results;
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
