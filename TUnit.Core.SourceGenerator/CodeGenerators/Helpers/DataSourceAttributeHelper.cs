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

        // Check if the attribute inherits from one of the base data source types
        return attributeClass.IsOrInherits("global::TUnit.Core.TestDataAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.IDataSourceAttribute");
    }
}