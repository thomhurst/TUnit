using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceAttributeHelper
{
    public static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
        {
            return false;
        }

        // Check if the attribute implements IDataSourceAttribute
        return attributeClass.AllInterfaces.Any(i => i.GloballyQualified() == "global::TUnit.Core.IDataSourceAttribute");
    }
}