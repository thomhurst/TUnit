using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Implementation of metadata filter matching logic extracted from TestBuilder.
/// Evaluates if test metadata could match an execution filter without building tests.
/// </summary>
internal sealed class MetadataFilterMatcher : IMetadataFilterMatcher
{
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
