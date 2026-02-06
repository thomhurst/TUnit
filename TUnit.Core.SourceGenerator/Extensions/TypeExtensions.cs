using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class TypeExtensions
{
    public static string GetMetadataName(this Type type)
    {
        return $"{type.Namespace}.{type.Name}";
    }

    public static IEnumerable<ISymbol> GetMembersIncludingBase(this ITypeSymbol namedTypeSymbol, bool reverse = true)
    {
        if (!reverse)
        {
            // Forward traversal - yield directly without allocations
            var symbol = namedTypeSymbol;
            while (symbol is not null && symbol.SpecialType != SpecialType.System_Object)
            {
                if (symbol is IErrorTypeSymbol)
                {
                    throw new Exception($"ErrorTypeSymbol for {symbol.Name} - Have you added any missing file sources to the compilation?");
                }

                foreach (var member in symbol.GetMembers())
                {
                    yield return member;
                }

                symbol = symbol.BaseType;
            }

            yield break;
        }

        // Reverse traversal - collect hierarchy, then yield from base to derived
        // Use stack to collect types (base to derived), then iterate members in forward order
        var typeStack = new Stack<ITypeSymbol>();
        var current = namedTypeSymbol;

        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            if (current is IErrorTypeSymbol)
            {
                throw new Exception($"ErrorTypeSymbol for {current.Name} - Have you added any missing file sources to the compilation?");
            }

            typeStack.Push(current);
            current = current.BaseType;
        }

        // Yield members from base to derived
        while (typeStack.Count > 0)
        {
            var type = typeStack.Pop();
            foreach (var member in type.GetMembers())
            {
                yield return member;
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetSelfAndBaseTypes(this INamedTypeSymbol namedTypeSymbol)
    {
        return [namedTypeSymbol, .. GetBaseTypes(namedTypeSymbol)];
    }

    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol.BaseType;

        while (type != null && type.SpecialType != SpecialType.System_Object)
        {
            yield return type;
            type = type.BaseType;
        }
    }

    public static IEnumerable<AttributeData> GetAttributesIncludingBaseTypes(this INamedTypeSymbol namedTypeSymbol)
    {
        return GetSelfAndBaseTypes(namedTypeSymbol).SelectMany(x => x.GetAttributes());
    }

    public static bool IsOrInherits(this INamedTypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.GloballyQualifiedNonGeneric() == typeName);
    }

    public static bool IsOrInherits(this INamedTypeSymbol namedTypeSymbol, [NotNullWhen(true)] ITypeSymbol? inheritedType)
    {
        if (inheritedType is null)
        {
            return false;
        }

        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => SymbolEqualityComparer.Default.Equals(x, inheritedType));
    }

    public static bool IsIEnumerable(this ITypeSymbol namedTypeSymbol, Compilation compilation, [NotNullWhen(true)] out ITypeSymbol? innerType)
    {
        var interfaces = namedTypeSymbol.TypeKind == TypeKind.Interface
            ? [(INamedTypeSymbol) namedTypeSymbol, .. namedTypeSymbol.AllInterfaces]
            : namedTypeSymbol.AllInterfaces.AsEnumerable();

        // Cache the special type lookup to avoid repeated calls
        var enumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

        foreach (var enumerable in interfaces
                     .Where(x => x.IsGenericType)
                     .Where(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, enumerableT)))
        {
            innerType = enumerable.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }

    public static string GloballyQualified(this ISymbol typeSymbol)
    {
        // Handle open generic types where type arguments are type parameters
        // This prevents invalid C# like List<T>, Dictionary<TKey, TValue>, T? where type parameters are undefined
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true, Arity: > 0 } namedTypeSymbol)
        {
            // Check if this is an unbound generic type or has type parameter arguments
            // Use multiple detection methods for robustness across Roslyn versions
            var hasTypeParameters = namedTypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
            var hasTypeParameterSymbols = namedTypeSymbol.TypeArguments.OfType<ITypeParameterSymbol>().Any();
            var isUnboundGeneric = namedTypeSymbol.IsUnboundGenericType;
            // Also detect generic type definitions by checking if type equals its OriginalDefinition
            var isGenericTypeDefinition = SymbolEqualityComparer.Default.Equals(namedTypeSymbol, namedTypeSymbol.OriginalDefinition);

            if (hasTypeParameters || hasTypeParameterSymbols || isUnboundGeneric || isGenericTypeDefinition)
            {
                // Special case for System.Nullable<> - Roslyn displays it as "T?" even for open generic
                if (namedTypeSymbol.SpecialType == SpecialType.System_Nullable_T ||
                    namedTypeSymbol.ConstructedFrom?.SpecialType == SpecialType.System_Nullable_T)
                {
                    return "global::System.Nullable<>";
                }

                // General case for other open generic types
                var typeBuilder = new StringBuilder(typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix));
                typeBuilder.Append('<');
                typeBuilder.Append(new string(',', namedTypeSymbol.TypeArguments.Length - 1));
                typeBuilder.Append('>');

                return typeBuilder.ToString();
            }
        }

        return typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    }

    /// <summary>
    /// Determines if a type is compiler-generated (e.g., async state machines, lambda closures).
    /// These types typically contain angle brackets in their names and cannot be represented in source code.
    /// </summary>
    public static bool IsCompilerGeneratedType(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        // Check the type name directly, not the display string
        // Compiler-generated types have names that start with '<' or contain '<>'
        // Examples: <BaseAsyncTest>d__0, <>c__DisplayClass0_0, <>f__AnonymousType0
        var typeName = typeSymbol.Name;

        // Compiler-generated types typically:
        // 1. Start with '<' (like <MethodName>d__0 for async state machines)
        // 2. Contain '<>' (like <>c for compiler-generated classes)
        // This won't match normal generic types like List<T> because those don't have '<' in the type name itself
        return typeName.StartsWith("<") || typeName.Contains("<>");
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

        if (namedTypeSymbol.IsUnboundGenericType)
        {
            return true;
        }

        if (!namedTypeSymbol.IsGenericType)
        {
            return false;
        }

        return namedTypeSymbol.TypeArguments.Any(IsGenericDefinition);
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

    /// <summary>
    /// Gets the nested class name with '+' separator (matching .NET Type.FullName convention).
    /// For example: OuterClass+InnerClass
    /// </summary>
    public static string GetNestedClassName(this INamedTypeSymbol typeSymbol)
    {
        var typeHierarchy = new List<string>();
        var currentType = typeSymbol;

        // Walk up the containing type chain
        while (currentType != null)
        {
            typeHierarchy.Add(currentType.Name);
            currentType = currentType.ContainingType;
        }

        // Reverse to get outer-to-inner order
        typeHierarchy.Reverse();

        // Join with '+' separator (matching .NET Type.FullName convention for nested types)
        return string.Join("+", typeHierarchy);
    }
}
