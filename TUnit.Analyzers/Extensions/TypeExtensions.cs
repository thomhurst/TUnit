using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetFullNameWithoutGenericArity(this Type type)
    {
        var name = type.FullName;
        
        var index = name.IndexOf('`');
        
        return index == -1 ? name : name.Substring(0, index);
    }
    
    public static IEnumerable<INamedTypeSymbol> GetSelfAndBaseTypes(this INamedTypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol;

        while (type != null)
        {
            yield return type;
            type = type.BaseType;
        }
    }
    
    public static bool IsOrInherits(this INamedTypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == typeName);
    }
    
    public static bool IsTestClass(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Any(x => x.IsTestMethod());
    }
}