using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

public static class PropertyExtensions
{
    public static bool TryGetClassDataAttribute(this IPropertySymbol propertySymbol,
        [NotNullWhen(true)] out AttributeData? attributeData)
    {
        attributeData = propertySymbol.GetAttributes()
            .FirstOrDefault(x =>
                x.AttributeClass?.GloballyQualifiedNonGeneric() == WellKnown.AttributeFullyQualifiedClasses.ClassDataSource.WithGlobalPrefix);

        return attributeData != null;
    }
}