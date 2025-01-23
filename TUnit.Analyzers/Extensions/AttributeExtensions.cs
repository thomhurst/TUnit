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
            x.AttributeClass?.GloballyQualifiedNonGeneric()
            == fullyQualifiedName);
    }
    
    public static Location? GetLocation(this AttributeData attributeData)
    {
        return attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }
    
    public static bool IsStandardHook(this AttributeData attributeData, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        if (
            SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.BeforeAttribute
                    .WithoutGlobalPrefix)))
        {
            hookType = HookType.Before;
            type = attributeData.AttributeClass!;
            hookLevel = (HookLevel?)Enum.Parse(typeof(HookLevel), attributeData.ConstructorArguments.First().ToCSharpString().Split('.').Last()); 
            return true;
        }
        
        if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.AfterAttribute
                    .WithoutGlobalPrefix)))
        {
            hookType = HookType.After;
            type = attributeData.AttributeClass!;
            hookLevel = (HookLevel?)Enum.Parse(typeof(HookLevel), attributeData.ConstructorArguments.First().ToCSharpString().Split('.').Last()); 
            return true;
        }

        hookType = null;
        type = null;
        hookLevel = null;
        return false;
    }
    
    public static bool IsEveryHook(this AttributeData attributeData, Compilation compilation, [NotNullWhen(true)] out INamedTypeSymbol? type, [NotNullWhen(true)] out HookLevel? hookLevel, [NotNullWhen(true)] out HookType? hookType)
    {
        if (
            SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.BeforeEveryAttribute
                    .WithoutGlobalPrefix)))
        {
            hookType = HookType.Before;
            type = attributeData.AttributeClass!;
            hookLevel = (HookLevel?)Enum.Parse(typeof(HookLevel), attributeData.ConstructorArguments.First().ToCSharpString().Split('.').Last());
            
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.AfterEveryAttribute
                    .WithoutGlobalPrefix)))
        {
            hookType = HookType.After;
            type = attributeData.AttributeClass!;
            hookLevel = (HookLevel?)Enum.Parse(typeof(HookLevel), attributeData.ConstructorArguments.First().ToCSharpString().Split('.').Last());
            
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
                   compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.Matrix
                       .WithoutGlobalPrefix));
    }
    
    public static bool IsMatrixDataSourceAttribute(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
            compilation.GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.MatrixDataSourceAttribute
                .WithoutGlobalPrefix));
    }
    
    public static bool IsDataSourceAttribute(this AttributeData? attributeData, Compilation compilation)
    {
        if (attributeData?.AttributeClass is null)
        {
            return false;
        }
        
        var dataAttributeInterface = compilation
            .GetTypeByMetadataName(WellKnown.AttributeFullyQualifiedClasses.IDataAttribute.WithoutGlobalPrefix);

        return attributeData.AttributeClass.AllInterfaces.Contains(dataAttributeInterface, SymbolEqualityComparer.Default);
    }

    public static string GetHookType(this AttributeData attributeData)
    {
        return attributeData.ConstructorArguments[0].ToCSharpString();
    }
}