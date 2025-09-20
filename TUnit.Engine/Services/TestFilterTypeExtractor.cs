using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Services;

/// <summary>
/// Extracts test class types from test filters to enable selective test discovery
/// </summary>
internal static class TestFilterTypeExtractor
{
    private static readonly Regex PathFilterRegex = new(@"^/([^/]+)/([^/]+)/([^/]+)(?:/|$)", RegexOptions.Compiled);
    
    private static readonly Lazy<Dictionary<string, List<System.Reflection.Assembly>>> AssemblyCache = 
        new(() => BuildAssemblyCache());
    
    private static readonly Lazy<Dictionary<string, Type>> TypeCache = 
        new(() => BuildTypeCache());
    
    private static Dictionary<string, List<System.Reflection.Assembly>> BuildAssemblyCache()
    {
        var cache = new Dictionary<string, List<System.Reflection.Assembly>>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name != null)
            {
                if (!cache.TryGetValue(name, out var list))
                {
                    list = new List<System.Reflection.Assembly>();
                    cache[name] = list;
                }
                list.Add(assembly);
            }
        }
        return cache;
    }
    
    private static Dictionary<string, Type> BuildTypeCache()
    {
        var cache = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
#pragma warning disable IL2026
                foreach (var type in assembly.GetExportedTypes())
#pragma warning restore IL2026
                {
                    if (type.FullName != null)
                    {
                        cache[type.FullName] = type;
                    }
                }
            }
            catch
            {
            }
        }
        return cache;
    }
    
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
                
                if (TypeCache.Value.TryGetValue(fullTypeName, out var cachedType))
                {
                    types.Add(cachedType);
                }
                else if (assemblyName != "*")
                {
                    if (AssemblyCache.Value.TryGetValue(assemblyName, out var assemblies))
                    {
                        foreach (var assembly in assemblies)
                        {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access
                            var type = assembly.GetType(fullTypeName, throwOnError: false);
#pragma warning restore IL2026
                            if (type != null)
                            {
                                types.Add(type);
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        return types.Count > 0 ? types : null;
    }
}