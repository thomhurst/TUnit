using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Enums;

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

    public static HookType GetHookType(this AttributeData attributeData)
    {
        return (HookType) Enum.ToObject(typeof(HookType), attributeData.ConstructorArguments[0].Value!);
    }
}