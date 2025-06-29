using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetMetadataName(this Type type)
    {
        return $"{type.Namespace}.{type.Name}";
    }

    public static string GetFullNameWithoutGenericArity(this Type type)
    {
        var name = type.FullName!;

        var index = name.IndexOf('`');

        return index == -1 ? name : name[..index];
    }

    public static IEnumerable<INamedTypeSymbol> GetSelfAndBaseTypes(this INamedTypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol;

        while (type != null && type.SpecialType != SpecialType.System_Object)
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

    public static string GloballyQualified(this ISymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

    public static string GloballyQualifiedNonGeneric(this ISymbol typeSymbol) =>
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

    public static bool IsIEnumerable(this ITypeSymbol namedTypeSymbol, Compilation compilation, [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        var interfaces = namedTypeSymbol.TypeKind == TypeKind.Interface
            ? [(INamedTypeSymbol) namedTypeSymbol, .. namedTypeSymbol.AllInterfaces]
            : namedTypeSymbol.AllInterfaces.AsEnumerable();

        foreach (var enumerable in interfaces
                     .Where(x => x.IsGenericType)
                     .Where(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))))
        {
            innerType = enumerable.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }

    public static bool IsDisposable(this ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.SpecialType == SpecialType.System_IDisposable);
    }

    public static bool IsAsyncDisposable(this ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.GloballyQualifiedNonGeneric() == "global::System.IAsyncDisposable");
    }

    public static bool IsCollectionType(this ITypeSymbol typeSymbol, Compilation compilation, [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            // Technically a collection but not what we're looking for
            innerType = null;
            return false;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            innerType = arrayTypeSymbol.ElementType;
            return true;
        }

        var enumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol
            && SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, enumerableT))
        {
            innerType = namedTypeSymbol.TypeArguments[0];
            return true;
        }

        if (typeSymbol.AllInterfaces
            .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, enumerableT))
            is { } enumerableType)
        {
            innerType = enumerableType.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }
}
