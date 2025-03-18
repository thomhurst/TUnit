using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Extensions;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class TypeExtensions
{
    private static readonly Dictionary<string, string> ReservedTypeKeywords = new()
    {
        { "System.Boolean", "bool" },
        { "System.Byte", "byte" },
        { "System.SByte", "sbyte" },
        { "System.Char", "char" },
        { "System.Decimal", "decimal" },
        { "System.Double", "double" },
        { "System.Single", "float" },
        { "System.Int32", "int" },
        { "System.UInt32", "uint" },
        { "System.Int64", "long" },
        { "System.UInt64", "ulong" },
        { "System.Int16", "short" },
        { "System.UInt16", "ushort" },
        { "System.Object", "object" },
        { "System.String", "string" }
    };
    
    public static string GetMetadataName(this Type type)
    {
        return $"{type.Namespace}.{type.Name}";
    }
    
    public static IEnumerable<ISymbol> GetMembersIncludingBase(this ITypeSymbol namedTypeSymbol, bool reverse = true)
    {
        var list = new List<ISymbol>();

        var symbol = namedTypeSymbol;

        while (symbol is not null)
        {
            if (symbol is IErrorTypeSymbol)
            {
                throw new Exception($"ErrorTypeSymbol for {symbol.Name} - Have you added any missing file sources to the compilation?");
            }

            if (symbol.SpecialType == SpecialType.System_Object)
            {
                break;
            }
            
            list.AddRange(reverse ? symbol.GetMembers().Reverse() : symbol.GetMembers());
            symbol = symbol.BaseType;
        }
        
        if (reverse)
        {
            list.Reverse();
        }

        return list;
    }
    
    public static IEnumerable<ITypeSymbol> GetSelfAndBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        return [namedTypeSymbol, ..GetBaseTypes(namedTypeSymbol)];
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol.BaseType;
        
        while (type != null && type.SpecialType != SpecialType.System_Object)
        {
            yield return type;
            type = type.BaseType;
        }
    }
    
    public static IEnumerable<AttributeData> GetAttributesIncludingBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        return GetSelfAndBaseTypes(namedTypeSymbol).SelectMany(x => x.GetAttributes());
    }

    public static bool IsOrInherits(this ITypeSymbol namedTypeSymbol, string typeName)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => x.GloballyQualifiedNonGeneric() == typeName);
    }
    
    public static bool IsOrInherits(this ITypeSymbol namedTypeSymbol, [NotNullWhen(true)] ITypeSymbol? inheritedType)
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
            ? [(INamedTypeSymbol)namedTypeSymbol, ..namedTypeSymbol.AllInterfaces]
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

    public static bool EnumerableGenericTypeIs(this ITypeSymbol enumerable, GeneratorAttributeSyntaxContext context,
        ImmutableArray<ITypeSymbol> parameterTypes, [NotNullWhen(true)] out ITypeSymbol? enumerableInnerType)
    {
        if (parameterTypes.IsDefaultOrEmpty)
        {
            enumerableInnerType = null;
            return false;
        }

        var genericEnumerableType =
            context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).ConstructUnboundGenericType();

        if (enumerable is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && namedTypeSymbol
                .ConstructUnboundGenericType().Equals(genericEnumerableType, SymbolEqualityComparer.Default))
        {
            enumerableInnerType = namedTypeSymbol.TypeArguments.First();
        }
        else
        {
            var enumerableInterface = enumerable.AllInterfaces.FirstOrDefault(x =>
                x.IsGenericType && x.ConstructUnboundGenericType()
                    .Equals(genericEnumerableType, SymbolEqualityComparer.Default));
            
            enumerableInnerType = enumerableInterface?.TypeArguments.FirstOrDefault();
        }
        
        if (enumerableInnerType is null)
        {
            enumerableInnerType = null;
            return false;
        }

        var firstParameterType = parameterTypes.FirstOrDefault();
        
        if (context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(enumerableInnerType, firstParameterType))
        {
            return true;
        }
        
        if (!enumerableInnerType.IsTupleType && firstParameterType is INamedTypeSymbol { IsGenericType:true })
        {
            return true;
        }

        if (enumerableInnerType.IsTupleType && enumerableInnerType is INamedTypeSymbol namedInnerType)
        {
            var tupleTypes = namedInnerType.TupleElements.Select(x => x.Type).ToImmutableArray();

            for (var index = 0; index < tupleTypes.Length; index++)
            {
                var tupleType = tupleTypes.ElementAtOrDefault(index);
                var parameterType = parameterTypes.ElementAtOrDefault(index);

                if (parameterType?.IsGenericDefinition() == true)
                {
                    continue;
                }
                
                if (!context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(tupleType, parameterType))
                {
                    return false;
                }
            }

            return true;
        }
        
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

        if (namedTypeSymbol.IsUnboundGenericType)
        {
            return true;
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
            is {} enumerableType)
        {
            innerType = enumerableType.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }
}