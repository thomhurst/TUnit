using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetFullNameWithoutGenericArity(this Type type)
    {
        var name = type.FullName;
        
        var index = name.IndexOf('`');
        
        return index == -1 ? name : name.Substring(0, index);
    }
    
    public static IEnumerable<ITypeSymbol> GetSelfAndBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol;

        while (type != null && type.SpecialType != SpecialType.System_Object)
        {
            yield return type;
            type = type.BaseType;
        }
    }
    
    public static bool IsOrInherits(this ITypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == typeName);
    }
    
    public static bool IsOrInheritsNonGeneric(this ITypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == typeName);
    }
}