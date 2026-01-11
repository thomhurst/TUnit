using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Hints extracted from a filter for pre-filtering test sources by type.
/// </summary>
internal readonly struct FilterHints
{
    /// <summary>
    /// The assembly name pattern from the filter (null if wildcard or unparseable).
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// The namespace pattern from the filter (null if wildcard or unparseable).
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// The class name pattern from the filter (null if wildcard or unparseable).
    /// </summary>
    public string? ClassName { get; init; }

    /// <summary>
    /// The method name pattern from the filter (null if wildcard or unparseable).
    /// </summary>
    public string? MethodName { get; init; }

    /// <summary>
    /// Whether the filter could be parsed to extract any hints.
    /// </summary>
    public bool HasHints => AssemblyName != null || Namespace != null || ClassName != null || MethodName != null;

    /// <summary>
    /// Check if a type could match this filter based on the hints.
    /// Returns true if no hints or if the type matches all available hints.
    /// </summary>
    public bool CouldTypeMatch(Type type)
    {
        // Check assembly name hint
        if (AssemblyName != null)
        {
            var typeAssemblyName = type.Assembly.GetName().Name;
            if (typeAssemblyName != AssemblyName)
            {
                return false;
            }
        }

        // Check namespace hint
        if (Namespace != null)
        {
            var typeNamespace = type.Namespace ?? string.Empty;
            if (typeNamespace != Namespace)
            {
                return false;
            }
        }

        // Check class name hint
        if (ClassName != null)
        {
            // Handle generic types (e.g., MyClass`1)
            if (type.Name != ClassName && !type.Name.StartsWith(ClassName + "`"))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Implementation of metadata filter matching logic extracted from TestBuilder.
/// Evaluates if test metadata could match an execution filter without building tests.
/// </summary>
internal sealed class MetadataFilterMatcher : IMetadataFilterMatcher
{
    /// <summary>
    /// Extract hints from a filter that can be used to pre-filter test sources by type.
    /// </summary>
#pragma warning disable TPEXP
    public static FilterHints ExtractFilterHints(ITestExecutionFilter? filter)
    {
        if (filter is not TreeNodeFilter treeFilter)
        {
            return default;
        }

        var filterString = treeFilter.Filter;
        if (string.IsNullOrEmpty(filterString))
        {
            return default;
        }

        // Strip property filters like [key=value]
        if (filterString.Contains('['))
        {
            filterString = System.Text.RegularExpressions.Regex.Replace(filterString, @"\[([^\]]*)\]", "");
        }

        // Parse path: /{assembly}/{namespace}/{className}/{methodName}
        var parts = filterString.Split('/');

        // Expected format: "", assembly, namespace, className, methodName
        // parts[0] is empty (before first /), parts[1] is assembly, etc.
        if (parts.Length < 2)
        {
            return default;
        }

        string? assemblyName = null;
        string? namespaceName = null;
        string? className = null;
        string? methodName = null;

        // Extract assembly name (parts[1])
        if (parts.Length > 1 && !IsWildcard(parts[1]))
        {
            assemblyName = parts[1];
        }

        // Extract namespace (parts[2])
        if (parts.Length > 2 && !IsWildcard(parts[2]))
        {
            namespaceName = parts[2];
        }

        // Extract class name (parts[3])
        if (parts.Length > 3 && !IsWildcard(parts[3]))
        {
            className = parts[3];
        }

        // Extract method name (parts[4])
        if (parts.Length > 4 && !IsWildcard(parts[4]))
        {
            methodName = parts[4];
        }

        return new FilterHints
        {
            AssemblyName = assemblyName,
            Namespace = namespaceName,
            ClassName = className,
            MethodName = methodName
        };
    }

    private static bool IsWildcard(string value)
    {
        return string.IsNullOrEmpty(value) || value == "*" || value.Contains('*') || value.Contains('?');
    }
#pragma warning restore TPEXP

    public bool CouldMatchFilter(TestMetadata metadata, ITestExecutionFilter? filter)
    {
#pragma warning disable TPEXP
        return filter switch
        {
            null => true,
            NopFilter => true,
            TreeNodeFilter treeFilter => CouldMatchTreeNodeFilter(treeFilter, metadata),
            TestNodeUidListFilter uidFilter => CouldMatchUidFilter(uidFilter, metadata),
            _ => true
        };
#pragma warning restore TPEXP
    }

    private static bool CouldMatchUidFilter(TestNodeUidListFilter filter, TestMetadata metadata)
    {
        var classMetadata = metadata.MethodMetadata.Class;
        var namespaceName = classMetadata.Namespace ?? "";
        var className = metadata.TestClassType.Name;
        var methodName = metadata.TestMethodName;

        foreach (var uid in filter.TestNodeUids)
        {
            var uidValue = uid.Value;
            if (uidValue.Contains(namespaceName) &&
                uidValue.Contains(className) &&
                uidValue.Contains(methodName))
            {
                return true;
            }
        }

        return false;
    }

#pragma warning disable TPEXP
    private static bool CouldMatchTreeNodeFilter(TreeNodeFilter filter, TestMetadata metadata)
    {
        var filterString = filter.Filter;

        if (string.IsNullOrEmpty(filterString))
        {
            return true;
        }

        TreeNodeFilter pathOnlyFilter;
        if (filterString.Contains('['))
        {
            var strippedFilterString = System.Text.RegularExpressions.Regex.Replace(filterString, @"\[([^\]]*)\]", "");
            pathOnlyFilter = CreateTreeNodeFilterViaReflection(strippedFilterString);
        }
        else
        {
            pathOnlyFilter = filter;
        }

        var path = BuildPathFromMetadata(metadata);
        var emptyPropertyBag = new PropertyBag();
        return pathOnlyFilter.MatchesFilter(path, emptyPropertyBag);
    }

    private static TreeNodeFilter CreateTreeNodeFilterViaReflection(string filterString)
    {
        var constructor = typeof(TreeNodeFilter).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];

        return (TreeNodeFilter)constructor.Invoke([filterString]);
    }
#pragma warning restore TPEXP

    private static string BuildPathFromMetadata(TestMetadata metadata)
    {
        var classMetadata = metadata.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var className = classMetadata.Name;
        var methodName = metadata.TestMethodName;

        return $"/{assemblyName}/{namespaceName}/{className}/{methodName}";
    }
}
