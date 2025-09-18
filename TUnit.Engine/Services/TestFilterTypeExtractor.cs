using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Services;

/// <summary>
/// Extracts test class types from test filters to enable selective test discovery
/// </summary>
internal static class TestFilterTypeExtractor
{
    private static readonly Regex PathFilterRegex = new(@"^/([^/]+)/([^/]+)/([^/]+)(?:/|$)", RegexOptions.Compiled);
    
    public static HashSet<Type>? ExtractTypesFromFilter(ITestExecutionFilter? filter)
    {
        if (filter == null)
        {
            return null;
        }
        
        return filter switch
        {
#pragma warning disable TPEXP
            NopFilter => null,
            TestNodeUidListFilter => null, // UIDs don't contain type info in a parseable way
            TreeNodeFilter treeNodeFilter => ExtractTypesFromTreeFilter(treeNodeFilter.Filter),
#pragma warning restore TPEXP
            _ => null
        };
    }
    
    private static HashSet<Type>? ExtractTypesFromTreeFilter(string? filterExpression)
    {
        if (string.IsNullOrWhiteSpace(filterExpression))
        {
            return null;
        }
        
        var types = new HashSet<Type>();
        
        // TreeNodeFilter uses path-based filtering like: /AssemblyName/Namespace/ClassName/MethodName
        // Extract class names from the filter
        var matches = PathFilterRegex.Matches(filterExpression);
        
        foreach (Match match in matches)
        {
            if (match.Success && match.Groups.Count >= 4)
            {
                var assemblyName = match.Groups[1].Value;
                var namespaceName = match.Groups[2].Value;
                var className = match.Groups[3].Value;
                
                // Skip wildcards
                if (assemblyName == "*" || namespaceName == "*" || className == "*")
                {
                    continue;
                }
                
                var fullTypeName = $"{namespaceName}.{className}";
                
                // Try to find the type in loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == assemblyName || assemblyName == "*")
                    {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
                        var type = assembly.GetType(fullTypeName, throwOnError: false);
#pragma warning restore IL2026
                        if (type != null)
                        {
                            types.Add(type);
                        }
                    }
                }
            }
        }
        
        return types.Count > 0 ? types : null;
    }
}