using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public static class CustomTestAttributeRetriever
{
    public static string GetBeforeTestAttributes(AttributeData[] attributes)
    {
        var applyToTestAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Any(i =>
            i.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == WellKnownFullyQualifiedClassNames.IBeforeTestAttribute.WithGlobalPrefix) == true);

        var fullyQualifiedAttributes = applyToTestAttributes.Select(x => x.AttributeClass!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix));
        
        var getAttributesCalls =
            fullyQualifiedAttributes
                .Select(x =>
                    $"..methodInfo.GetCustomAttributes<{x}>(), ..classType.GetCustomAttributes<{x}>()"
                );
        
        return string.Join(", ", getAttributesCalls);
    }
    
    public static string GetAfterTestAttributes(AttributeData[] attributes)
    {
        var applyToTestAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Any(i =>
                i.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnownFullyQualifiedClassNames.IAfterTestAttribute.WithGlobalPrefix) == true);

        var fullyQualifiedAttributes = applyToTestAttributes.Select(x => x.AttributeClass!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix));
        
        var getAttributesCalls =
            fullyQualifiedAttributes
                .Select(x =>
                    $"..methodInfo.GetCustomAttributes<{x}>(), ..classType.GetCustomAttributes<{x}>()"
                );
        
        return string.Join(", ", getAttributesCalls);
    }
}