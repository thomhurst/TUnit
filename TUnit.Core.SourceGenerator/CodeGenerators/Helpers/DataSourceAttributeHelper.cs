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

        var name = attributeClass.Name;
        var fullyQualifiedName = attributeClass.GloballyQualified();
        
        // Check by simple name first (more flexible)
        if (name == "ArgumentsAttribute" ||
            name == "MethodDataSourceAttribute" ||
            name == "InstanceMethodDataSourceAttribute" ||
            name == "ClassDataSourceAttribute")
        {
            return true;
        }
        
        // Check by fully qualified name (backup)
        return fullyQualifiedName.Contains("TUnit.Core.ArgumentsAttribute") ||
               fullyQualifiedName.Contains("TUnit.Core.MethodDataSourceAttribute") ||
               fullyQualifiedName.Contains("TUnit.Core.InstanceMethodDataSourceAttribute") ||
               fullyQualifiedName.Contains("TUnit.Core.ClassDataSourceAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
               attributeClass.IsOrInherits("global::TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute");
    }
}