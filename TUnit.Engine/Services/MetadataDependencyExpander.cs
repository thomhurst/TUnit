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
        var queue = new Queue<TestMetadata>(matchingMetadata);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var dependency in current.Dependencies)
            {
                foreach (var candidateMetadata in metadataList)
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
