using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers.Extensions;

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