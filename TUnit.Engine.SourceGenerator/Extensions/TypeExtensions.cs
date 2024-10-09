﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.Extensions;

internal static class TypeExtensions
{
    public static IEnumerable<ISymbol> GetMembersIncludingBase(this ITypeSymbol namedTypeSymbol, bool reverse = true)
    {
        var list = new List<ISymbol>();

        var symbol = namedTypeSymbol;

        while (symbol is not null)
        {
            if (symbol is IErrorTypeSymbol)
            {
                throw new Exception("ErrorTypeSymbol - Have you added any missing file sources to the compilation?");
            }

            if (symbol.SpecialType == SpecialType.System_Object)
            {
                break;
            }
            
            list.AddRange(reverse ? symbol.GetMembers().Reverse() : symbol.GetMembers());
            symbol = symbol.BaseType;
        }
        
        if(reverse)
        {
            list.Reverse();
        }

        return list;
    }

    public static IEnumerable<ITypeSymbol> GetSelfAndBaseTypes(this ITypeSymbol namedTypeSymbol)
    {
        var type = namedTypeSymbol;
        
        while (type != null)
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
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == typeName);
    }
    
    public static bool IsOrInherits(this ITypeSymbol namedTypeSymbol, ITypeSymbol inheritedType)
    {
        return namedTypeSymbol
            .GetSelfAndBaseTypes()
            .Any(x => SymbolEqualityComparer.Default.Equals(x, inheritedType));
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

        if (enumerable is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType && namedTypeSymbol
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

        if (context.SemanticModel.Compilation.HasImplicitConversion(enumerableInnerType,
                parameterTypes.FirstOrDefault()))
        {
            return true;
        }

        if (enumerableInnerType.IsTupleType && enumerableInnerType is INamedTypeSymbol namedInnerType)
        {
            var tupleTypes = namedInnerType.TupleUnderlyingType?.TypeArguments ?? namedInnerType.TypeArguments;

            for (var index = 0; index < tupleTypes.Length; index++)
            {
                var tupleType = tupleTypes.ElementAtOrDefault(index);
                var parameterType = parameterTypes.ElementAtOrDefault(index);

                if (!context.SemanticModel.Compilation.HasImplicitConversion(tupleType, parameterType))
                {
                    return false;
                }
            }

            return true;
        }
        
        return false;
    }
    
    public static string GloballyQualified(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    
    public static string GloballyQualifiedNonGeneric(this ITypeSymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
}