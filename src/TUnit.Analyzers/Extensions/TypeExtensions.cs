using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetMetadataName(this Type type)
    {
        return string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
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

    public static string GloballyQualified(this ISymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    }

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
            ? new[] { (INamedTypeSymbol)namedTypeSymbol }.Concat(namedTypeSymbol.AllInterfaces)
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

    public static bool HasVisibleMutability(this ITypeSymbol type)
    {
        // Value types and string are safe — no shared-state concern
        if (type.IsValueType || type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        // Arrays are always mutable (elements can be reassigned)
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        // Delegates are immutable
        if (type.TypeKind == TypeKind.Delegate)
        {
            return false;
        }

        // Generic type parameters and interfaces — we can't know the concrete type, be conservative
        if (type is ITypeParameterSymbol || type.TypeKind == TypeKind.Interface)
        {
            return true;
        }

        // Walk the named type hierarchy looking for mutability evidence
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var t in namedType.GetSelfAndBaseTypes())
            {
                foreach (var member in t.GetMembers())
                {
                    // Property with a non-init setter (any accessibility)
                    if (member is IPropertySymbol { SetMethod: { } setter } && !setter.IsInitOnly)
                    {
                        return true;
                    }

                    // Non-readonly, non-const, non-compiler-generated field
                    if (member is IFieldSymbol { IsReadOnly: false, IsConst: false, IsImplicitlyDeclared: false } field
                        && field.DeclaredAccessibility == Accessibility.Public)
                    {
                        return true;
                    }

                    // Lazy<T> field or property (any accessibility) — deferred mutation
                    if (member is IFieldSymbol lazyField
                        && lazyField.Type is INamedTypeSymbol { IsGenericType: true } lazyFieldType
                        && lazyFieldType.ConstructedFrom.ToDisplayString() == "System.Lazy<T>")
                    {
                        return true;
                    }

                    if (member is IPropertySymbol lazyProp
                        && lazyProp.Type is INamedTypeSymbol { IsGenericType: true } lazyPropType
                        && lazyPropType.ConstructedFrom.ToDisplayString() == "System.Lazy<T>")
                    {
                        return true;
                    }
                }
            }

            // No mutability evidence found
            return false;
        }

        // Unknown kind — be conservative
        return true;
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
