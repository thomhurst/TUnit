using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Equality comparer for TestMetadata based on unique test properties.
/// Used to compare metadata instances that represent the same test.
/// </summary>
internal sealed class TestMetadataEqualityComparer : IEqualityComparer<TestMetadata>
{
    public static readonly TestMetadataEqualityComparer Instance = new();

    public bool Equals(TestMetadata? x, TestMetadata? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // For dynamic tests, also compare DynamicTestIndex to distinguish
        // multiple dynamic tests targeting the same method with different arguments
        if (x is IDynamicTestMetadata xDynamic && y is IDynamicTestMetadata yDynamic)
        {
            if (xDynamic.DynamicTestIndex != yDynamic.DynamicTestIndex)
            {
                return false;
            }
        }

        return x.TestClassType == y.TestClassType &&
               x.TestMethodName == y.TestMethodName &&
               x.TestName == y.TestName;
    }

    public int GetHashCode(TestMetadata obj)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (obj.TestClassType?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.TestMethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.TestName?.GetHashCode() ?? 0);

            // Include DynamicTestIndex in hash for dynamic tests
            if (obj is IDynamicTestMetadata dynamicMetadata)
            {
                hash = hash * 31 + dynamicMetadata.DynamicTestIndex;
            }

            return hash;
        }
    }
}

/// <summary>
/// Expands filtered test metadata to include all transitive dependencies.
/// Ensures that when tests are filtered, their dependency tests are also included.
/// </summary>
internal sealed class MetadataDependencyExpander
{
    private readonly IMetadataFilterMatcher _filterMatcher;

    public MetadataDependencyExpander(IMetadataFilterMatcher filterMatcher)
    {
        _filterMatcher = filterMatcher ?? throw new ArgumentNullException(nameof(filterMatcher));
    }

    public HashSet<TestMetadata> ExpandToIncludeDependencies(
        IEnumerable<TestMetadata> allMetadata,
        ITestExecutionFilter? filter)
    {
        var metadataList = allMetadata.ToList();

        if (filter == null)
        {
            return new HashSet<TestMetadata>(metadataList, TestMetadataEqualityComparer.Instance);
        }

        var matchingMetadata = metadataList
            .Where(m => _filterMatcher.CouldMatchFilter(m, filter))
            .ToList();

        var result = new HashSet<TestMetadata>(matchingMetadata, TestMetadataEqualityComparer.Instance);

        // Fast path: check if any matching metadata has dependencies
        var hasDependencies = false;
        foreach (var m in matchingMetadata)
        {
            if (m.Dependencies.Length > 0)
            {
                hasDependencies = true;
                break;
            }
        }

        if (!hasDependencies)
        {
            // No dependencies to expand - return early
            return result;
        }

        // Build indexes for O(1) dependency lookup instead of O(n) scanning
        // Index by class type for class-level dependencies
        var byClassType = new Dictionary<Type, List<TestMetadata>>();
        // Index by (class type, method name) for specific method dependencies
        var byClassAndMethod = new Dictionary<(Type, string), List<TestMetadata>>();
        // Index by method name for same-class dependencies
        var byMethodName = new Dictionary<string, List<TestMetadata>>();

        foreach (var metadata in metadataList)
        {
            var classType = metadata.TestClassType;
            var methodName = metadata.TestMethodName;

            // Add to class type index
            if (!byClassType.TryGetValue(classType, out var classTypeList))
            {
                classTypeList = [];
                byClassType[classType] = classTypeList;
            }
            classTypeList.Add(metadata);

            // Add to class+method index
            var classMethodKey = (classType, methodName);
            if (!byClassAndMethod.TryGetValue(classMethodKey, out var classMethodList))
            {
                classMethodList = [];
                byClassAndMethod[classMethodKey] = classMethodList;
            }
            classMethodList.Add(metadata);

            // Add to method name index
            if (!byMethodName.TryGetValue(methodName, out var methodNameList))
            {
                methodNameList = [];
                byMethodName[methodName] = methodNameList;
            }
            methodNameList.Add(metadata);
        }

        var queue = new Queue<TestMetadata>(matchingMetadata);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dependency in current.Dependencies)
            {
                // Get candidate list based on dependency type for O(1) lookup
                IEnumerable<TestMetadata> candidates;

                if (dependency.ClassType != null && !string.IsNullOrEmpty(dependency.MethodName))
                {
                    // Specific class and method - use most specific index
                    var key = (dependency.ClassType, dependency.MethodName!);
                    if (byClassAndMethod.TryGetValue(key, out var list))
                    {
                        candidates = list;
                    }
                    else
                    {
                        // For generic types or inheritance, fall back to class index
                        candidates = byClassType.TryGetValue(dependency.ClassType, out var classList)
                            ? classList
                            : [];
                    }
                }
                else if (dependency.ClassType != null)
                {
                    // Class-level dependency - all tests in the class
                    candidates = byClassType.TryGetValue(dependency.ClassType, out var list)
                        ? list
                        : [];
                }
                else if (!string.IsNullOrEmpty(dependency.MethodName))
                {
                    // Same-class dependency by method name - look up by method name, then filter by class
                    if (byMethodName.TryGetValue(dependency.MethodName!, out var list))
                    {
                        candidates = list.Where(m => m.TestClassType == current.TestClassType);
                    }
                    else
                    {
                        candidates = [];
                    }
                }
                else
                {
                    // Fallback for edge cases
                    candidates = metadataList;
                }

                foreach (var candidateMetadata in candidates)
                {
                    if (dependency.Matches(candidateMetadata, current))
                    {
                        if (result.Add(candidateMetadata))
                        {
                            queue.Enqueue(candidateMetadata);
                        }
                    }
                }
            }
        }

        return result;
    }
}
