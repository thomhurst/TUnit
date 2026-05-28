using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

/// <summary>
/// Hints extracted from a filter for pre-filtering test sources by type.
/// </summary>
internal readonly struct FilterHints
{
    /// <summary>
    /// The assembly name pattern from the filter (null if non-literal or unparseable).
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// The namespace pattern from the filter (null if non-literal or unparseable).
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// The class name pattern from the filter (null if non-literal or unparseable).
    /// </summary>
    public string? ClassName { get; init; }

    /// <summary>
    /// The method name pattern from the filter (null if non-literal or unparseable).
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
            // The filter ClassName may be "OuterClass+InnerClass" or just "InnerClass"
            // type.Name is just the innermost name
            var nestedName = TUnit.Core.Extensions.TestContextExtensions.GetNestedTypeName(type);
            if (nestedName != ClassName
                && !nestedName.StartsWith(ClassName + "`")
                && !nestedName.EndsWith("+" + ClassName)
                && !nestedName.StartsWith(ClassName + "+")
                && type.Name != ClassName
                && !type.Name.StartsWith(ClassName + "`"))
            {
                return false;
            }
        }

        return true;
    }

    public bool CouldMatch(string testClassName, string testMethodName)
    {
        if (ClassName != null)
        {
            if (testClassName != ClassName
                && !testClassName.StartsWith(ClassName + "`")
                && !testClassName.EndsWith("+" + ClassName)
                && !testClassName.StartsWith(ClassName + "+"))
            {
                return false;
            }
        }

        if (MethodName != null)
        {
            if (testMethodName != MethodName)
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
#pragma warning disable TPEXP
    private static readonly ConstructorInfo _treeNodeFilterConstructor =
        typeof(TreeNodeFilter).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string)], null)!;
#pragma warning restore TPEXP

    private static readonly PropertyBag _emptyPropertyBag = new();

    private static readonly ConcurrentDictionary<string, string> _strippedFilterCache = new();

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
        // Expected format: "", assembly, namespace, className, methodName
        // segment[0] is empty (before first /), segment[1] is assembly, etc.
        // FilterHints only retains segments 1-4, so we only materialize those.
#if NET8_0_OR_GREATER
        var filterSpan = filterString.AsSpan();

        // Up to 5 useful segments (empty, assembly, namespace, className, methodName);
        // a 6th range absorbs any trailing path so the earlier segments stay intact.
        Span<Range> ranges = stackalloc Range[6];
        var count = filterSpan.Split(ranges, '/');

        // Need at least the leading empty segment plus the assembly segment.
        if (count < 2)
        {
            return default;
        }

        return new FilterHints
        {
            AssemblyName = ExtractSegment(filterSpan, ranges, count, 1),
            Namespace = ExtractSegment(filterSpan, ranges, count, 2),
            ClassName = ExtractSegment(filterSpan, ranges, count, 3),
            MethodName = ExtractSegment(filterSpan, ranges, count, 4)
        };
    }

    private static string? ExtractSegment(ReadOnlySpan<char> filterSpan, ReadOnlySpan<Range> ranges, int count, int index)
    {
        if (index >= count)
        {
            return null;
        }

        var segment = filterSpan[ranges[index]];
        return IsNonLiteralSegment(segment) ? null : segment.ToString();
    }
#else
        var parts = filterString.Split('/');

        if (parts.Length < 2)
        {
            return default;
        }

        return new FilterHints
        {
            AssemblyName = ExtractSegment(parts, 1),
            Namespace = ExtractSegment(parts, 2),
            ClassName = ExtractSegment(parts, 3),
            MethodName = ExtractSegment(parts, 4)
        };
    }

    private static string? ExtractSegment(string[] parts, int index)
    {
        if (index >= parts.Length)
        {
            return null;
        }

        var segment = parts[index];
        return IsNonLiteralSegment(segment) ? null : segment;
    }
#endif

    // Characters that make a TreeNodeFilter path segment non-literal:
    //   * ?           wildcards
    //   ( ) | & !     grouping / logical operators
    //   \             escape character
    // Property-bag brackets [ ] are stripped before ExtractFilterHints runs (line above).
    // Characters that are NOT MTP operators and must stay literal: + (nested classes),
    // . (namespaces), < > , space (generic class names), ^ (no meaning in the grammar).
    // A segment containing any operator cannot be safely compared with string equality —
    // hints are skipped for it, and MTP's TreeNodeFilter does the authoritative match
    // downstream in CouldMatchTreeNodeFilter.
#if NET8_0_OR_GREATER
    private static readonly SearchValues<char> _filterOperatorChars =
        SearchValues.Create("*?()|&!\\");
#else
    private static readonly char[] _filterOperatorChars = { '*', '?', '(', ')', '|', '&', '!', '\\' };
#endif

#if NET8_0_OR_GREATER
    private static bool IsNonLiteralSegment(ReadOnlySpan<char> value)
    {
        return value.IsEmpty || value.IndexOfAny(_filterOperatorChars) >= 0;
    }
#else
    private static bool IsNonLiteralSegment(string value)
    {
        return string.IsNullOrEmpty(value) || value.IndexOfAny(_filterOperatorChars) >= 0;
    }
#endif
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
        var methodName = metadata.TestMethodName;

        // Build the full class name including nested type hierarchy (e.g., Outer+Inner)
        // This matches the format used by TestIdentifierService.WriteTypeNameWithGenerics
        var classNameForMatching = BuildClassNameForMatching(metadata.TestClassType);

        // Build expected prefix: {Namespace}.{ClassName} or just {ClassName} for empty namespace
        // The class name may be followed by '.', '<', or '(' depending on:
        // - '.' for regular classes (e.g., MyClass.0.0.Method)
        // - '<' for generic classes (e.g., MyClass<System.Int32>.0.0.Method)
        // - '(' for classes with constructor parameters (e.g., MyClass(System.String).0.0.Method)
        var expectedClassPrefix = string.IsNullOrEmpty(namespaceName)
            ? classNameForMatching
            : $"{namespaceName}.{classNameForMatching}";

        foreach (var uid in filter.TestNodeUids)
        {
            var uidValue = uid.Value;

            // Check for exact namespace.classname prefix to avoid matching
            // same class name in different namespaces
            if (!uidValue.StartsWith(expectedClassPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Verify the character after the class name is a valid boundary: '.', '<', or '('
            var indexAfterPrefix = expectedClassPrefix.Length;
            if (indexAfterPrefix < uidValue.Length)
            {
                var charAfterPrefix = uidValue[indexAfterPrefix];
                if (charAfterPrefix != '.' && charAfterPrefix != '<' && charAfterPrefix != '(')
                {
                    // Not a valid boundary - this could be a substring match
                    // e.g., "ABCV" matching "ABCVC"
                    continue;
                }
            }

            // Check for method name with word boundaries
            // Method names are preceded by '.' and followed by '.', '<', or '('
            if (!HasMethodNameMatch(uidValue, methodName))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Builds the class name for matching as it appears in UIDs.
    /// Handles nested types (Outer+Inner) and generic types with their type arguments.
    /// This matches the format used by TestIdentifierService.WriteTypeNameWithGenerics.
    /// </summary>
    private static string BuildClassNameForMatching(Type type)
    {
        // Fast path: non-nested, non-generic types
        if (type.DeclaringType == null && !type.IsGenericType)
        {
            return type.Name;
        }

        // Collect the nested-type chain (inner -> outer) without allocating per-segment strings,
        // then emit it directly into the result builder. Matches TestIdentifierService.WriteTypeNameWithGenerics.
        var typeHierarchy = new ValueListBuilder<Type>([null!, null!, null!, null!]);
        var resultVsb = new ValueStringBuilder(stackalloc char[256]);
        try
        {
            var currentType = type;
            while (currentType != null)
            {
                typeHierarchy.Append(currentType);
                currentType = currentType.DeclaringType;
            }

            // Reverse to get outer-to-inner order and join with '+'.
            for (var i = typeHierarchy.Length - 1; i >= 0; i--)
            {
                if (i < typeHierarchy.Length - 1)
                {
                    resultVsb.Append('+');
                }
                TypeNameHelper.AppendTypeNameWithGenericArgs(ref resultVsb, typeHierarchy[i]);
            }
            return resultVsb.ToString();
        }
        finally
        {
            resultVsb.Dispose();
            typeHierarchy.Dispose();
        }
    }

    private static bool HasMethodNameMatch(string uidValue, string methodName)
    {
        // Method name patterns with proper boundaries:
        // .{MethodName}. (most common: after class indices)
        // .{MethodName}< (generic method)
        // .{MethodName}( (method with parameter types in signature)
        ReadOnlySpan<char> validSuffixes = ['.', '<', '('];

        var searchStart = 0;
        int index;
        while ((index = uidValue.IndexOf(methodName, searchStart, StringComparison.Ordinal)) >= 0)
        {
            // Method name must be preceded by '.'
            var prefixOk = index > 0 && uidValue[index - 1] == '.';

            // Check suffix boundary
            var suffixIndex = index + methodName.Length;
            var suffixOk = suffixIndex < uidValue.Length &&
                           validSuffixes.Contains(uidValue[suffixIndex]);

            if (prefixOk && suffixOk)
            {
                return true;
            }

            searchStart = index + 1;
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
            var strippedFilterString = _strippedFilterCache.GetOrAdd(filterString,
                static fs => System.Text.RegularExpressions.Regex.Replace(fs, @"\[([^\]]*)\]", ""));
            pathOnlyFilter = CreateTreeNodeFilterViaReflection(strippedFilterString);
        }
        else
        {
            pathOnlyFilter = filter;
        }

        var path = BuildPathFromMetadata(metadata);
        return pathOnlyFilter.MatchesFilter(path, _emptyPropertyBag);
    }

    private static TreeNodeFilter CreateTreeNodeFilterViaReflection(string filterString)
    {
        return (TreeNodeFilter)_treeNodeFilterConstructor.Invoke([filterString]);
    }
#pragma warning restore TPEXP

    private static string BuildPathFromMetadata(TestMetadata metadata)
    {
        if (metadata.CachedFilterPath is { } cached)
        {
            return cached;
        }

        var classMetadata = metadata.MethodMetadata.Class;
        var assemblyName = classMetadata.Assembly.Name ?? metadata.TestClassType.Assembly.GetName().Name ?? "*";
        var namespaceName = classMetadata.Namespace ?? "*";
        var className = TestFilterService.GetNestedClassName(classMetadata);
        var methodName = metadata.TestMethodName;

        var path = $"/{assemblyName}/{namespaceName}/{className}/{methodName}";
        metadata.CachedFilterPath = path;
        return path;
    }
}
