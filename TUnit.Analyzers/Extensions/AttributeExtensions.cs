using System.Collections.Immutable;
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
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == fullyQualifiedName);
    }
    
    public static Location? GetLocation(this AttributeData attributeData)
    {
        return attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation();
    }
    
    public static bool IsNonGlobalHook(this AttributeData attributeData)
    {
        var displayString = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        return displayString == WellKnown.AttributeFullyQualifiedClasses.BeforeAttribute ||
               displayString == WellKnown.AttributeFullyQualifiedClasses.AfterAttribute;
    }
    
    public static bool IsGlobalHook(this AttributeData attributeData)
    {
        var displayString = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        return displayString == WellKnown.AttributeFullyQualifiedClasses.BeforeEveryAttribute ||
               displayString == WellKnown.AttributeFullyQualifiedClasses.AfterEveryAttribute;
    }

    public static string GetHookType(this AttributeData attributeData)
    {
        return attributeData.ConstructorArguments[0].ToCSharpString();
    }
}