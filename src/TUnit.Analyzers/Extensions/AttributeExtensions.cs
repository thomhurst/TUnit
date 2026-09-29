using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

public static class AttributeExtensions
{
    public static AttributeData? Get(this ImmutableArray<AttributeData> attributeDatas, string fullyQualifiedName)
    {
        if (!fullyQualifiedName.StartsWith("global::"))
        {
            fullyQualifiedName = $"global::{fullyQualifiedName}";
        }

        return attributeDatas.FirstOrDefault(x =>
            x.AttributeClass?.IsGloballyQualifiedNonGeneric(fullyQualifiedName) == true);
    }

    public static Location? GetLocation(this AttributeData attributeData)
    {
        return attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }

    public static bool IsStandardHook(this AttributeData attributeData, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
        => IsStandardHook(attributeData, TUnitSymbols.For(compilation), out type, out hookLevel, out hookType);

    internal static bool IsStandardHook(this AttributeData attributeData, TUnitSymbols symbols, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        // The null check stops an unresolved attribute class matching a hook symbol that is missing from the compilation.
        if (attributeData.AttributeClass is not null
            && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.BeforeAttribute))
        {
            hookType = HookType.Before;
            type = attributeData.AttributeClass!;
            hookLevel = ParseHookLevel(attributeData);
            return true;
        }

        if (attributeData.AttributeClass is not null
            && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.AfterAttribute))
        {
            hookType = HookType.After;
            type = attributeData.AttributeClass!;
            hookLevel = ParseHookLevel(attributeData);
            return true;
        }

        hookType = null;
        type = null;
        hookLevel = null;
        return false;
    }

    public static bool IsEveryHook(this AttributeData attributeData, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
        => IsEveryHook(attributeData, TUnitSymbols.For(compilation), out type, out hookLevel, out hookType);

    internal static bool IsEveryHook(this AttributeData attributeData, TUnitSymbols symbols, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        if (attributeData.AttributeClass is not null
            && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.BeforeEveryAttribute))
        {
            hookType = HookType.Before;
            type = attributeData.AttributeClass!;
            hookLevel = ParseHookLevel(attributeData);

            return true;
        }

        if (attributeData.AttributeClass is not null
            && SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbols.AfterEveryAttribute))
        {
            hookType = HookType.After;
            type = attributeData.AttributeClass!;
            hookLevel = ParseHookLevel(attributeData);

            return true;
        }

        hookType = null;
        type = null;
        hookLevel = null;
        return false;
    }

    public static bool IsMatrixAttribute(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
            TUnitSymbols.For(compilation).MatrixAttribute);
    }

    public static bool IsMatrixDataSourceAttribute(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
            TUnitSymbols.For(compilation).MatrixDataSourceAttribute);
    }

    public static bool IsCombinedDataSourceAttribute(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
            TUnitSymbols.For(compilation).CombinedDataSourceAttribute);
    }

    public static bool IsDataSourceAttribute(this AttributeData? attributeData, Compilation compilation)
    {
        if (attributeData?.AttributeClass is null)
        {
            return false;
        }

        var dataAttributeInterface = TUnitSymbols.For(compilation).DataSourceAttributeInterface!;

        return attributeData.AttributeClass.AllInterfaces.Contains(dataAttributeInterface, SymbolEqualityComparer.Default);
    }

    public static bool IsClassConstructorAttribute(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass is null)
        {
            return false;
        }

        var baseType = attributeData.AttributeClass;
        while (baseType != null)
        {
            if (baseType.Name == "BaseClassConstructorAttribute" ||
                baseType.Name == "ClassConstructorAttribute")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private static HookLevel ParseHookLevel(AttributeData attributeData)
    {
        var span = attributeData.ConstructorArguments.First().ToCSharpString().AsSpan();
        var lastDot = span.LastIndexOf('.');
        return Enum.Parse<HookLevel>(lastDot < 0 ? span : span[(lastDot + 1)..]);
    }
}
