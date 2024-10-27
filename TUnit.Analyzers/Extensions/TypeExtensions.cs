using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetFullNameWithoutGenericArity(this Type type)
    {
        var name = type.FullName!;
        
        var index = name.IndexOf('`');
        
        return index == -1 ? name : name[..index];
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
            .Any(x => x.GloballyQualified() == typeName);
    }
    
    public static bool IsTestClass(this INamedTypeSymbol namedTypeSymbol, Compilation compilation)
    {
        return namedTypeSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Any(x => x.IsTestMethod(compilation));
    }
    
    public static bool IsEnumerable(this ITypeSymbol type, SymbolAnalysisContext context, [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        var enumerableT = context.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).ConstructUnboundGenericType();

        if (type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && namedTypeSymbol
                .ConstructUnboundGenericType().Equals(enumerableT, SymbolEqualityComparer.Default))
        {
            innerType = namedTypeSymbol.TypeArguments.First();
            return true;
        }
        
        var enumerableInterface = type
            .AllInterfaces
            .FirstOrDefault(x => x.IsGenericType && x.ConstructUnboundGenericType().Equals(enumerableT, SymbolEqualityComparer.Default));

        if (enumerableInterface != null)
        {
            innerType = enumerableInterface.TypeArguments.First();
            return true;
        }

        innerType = null;
        return false;
    }

    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
    
    public static bool IsGenericDefinition(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is ITypeParameterSymbol)
        {
            return true;
        }
        
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        return namedTypeSymbol.TypeArguments.Any(IsGenericDefinition);
    }
}