using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    /// <summary>
    /// Safely gets a constructor argument value, returning null if the argument is in an error state.
    /// </summary>
    public static T? SafeGetConstructorArgument<T>(this AttributeData attributeData, int index)
    {
        if (index < 0 || index >= attributeData.ConstructorArguments.Length)
        {
            return default;
        }

        var argument = attributeData.ConstructorArguments[index];
        if (argument.Kind == TypedConstantKind.Error)
        {
            return default;
        }

        if (argument.Value is T value)
        {
            return value;
        }

        return default;
    }

    /// <summary>
    /// Checks if any constructor arguments are in an error state.
    /// </summary>
    public static bool HasErrorArguments(this AttributeData attributeData)
    {
        return attributeData.ConstructorArguments.Any(arg => arg.Kind == TypedConstantKind.Error);
    }

    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualifiedNonGeneric();
    }

    public static bool IsTestAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualified() == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix;
    }

    public static bool IsDataSourceAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.AllInterfaces.Any(x =>
                   x.GloballyQualified() == WellKnownFullyQualifiedClassNames.IDataSourceAttribute.WithGlobalPrefix)
               == true;
    }
    
    public static bool IsTypedDataSourceAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.AllInterfaces.Any(x =>
                   x.IsGenericType && 
                   x.ConstructedFrom.GloballyQualified() == WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1")
               == true;
    }
    
    public static ITypeSymbol? GetTypedDataSourceType(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return null;
        }

        var typedInterface = attributeData.AttributeClass.AllInterfaces
            .FirstOrDefault(x => x.IsGenericType && 
                x.ConstructedFrom.GloballyQualified() == WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");
                
        return typedInterface?.TypeArguments.FirstOrDefault();
    }

    public static bool IsNonGlobalHook(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.BeforeAttribute
                       .WithoutGlobalPrefix))
               || SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.AfterAttribute
                       .WithoutGlobalPrefix));
    }

    public static bool IsGlobalHook(this AttributeData attributeData, Compilation compilation)
    {
        return SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.BeforeEveryAttribute
                       .WithoutGlobalPrefix))
               || SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass,
                   compilation.GetTypeByMetadataName(WellKnownFullyQualifiedClassNames.AfterEveryAttribute
                       .WithoutGlobalPrefix));
    }
}
