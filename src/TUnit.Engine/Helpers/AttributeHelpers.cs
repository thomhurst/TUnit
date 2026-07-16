using TUnit.Core;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Shared attribute-extraction helpers used by the dynamic-discovery code paths
/// (AOT, reflection, and runtime registration) so they stay in sync.
/// </summary>
internal static class AttributeHelpers
{
    /// <summary>
    /// Extracts <see cref="TestDependency"/> entries from a materialised attribute list.
    /// Returns the shared empty array when no <see cref="DependsOnAttribute"/> is present.
    /// </summary>
    public static TestDependency[] ExtractDependencies(List<Attribute> attributes)
    {
        List<TestDependency>? dependencies = null;

        foreach (var attribute in attributes)
        {
            if (attribute is DependsOnAttribute dependsOn)
            {
                (dependencies ??= []).Add(dependsOn.ToTestDependency());
            }
        }

        return dependencies?.ToArray() ?? [];
    }
}
