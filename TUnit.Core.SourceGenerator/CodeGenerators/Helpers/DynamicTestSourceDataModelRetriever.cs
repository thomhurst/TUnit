using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class DynamicTestSourceDataModelRetriever
{
    public static DynamicTestSourceDataModel ParseDynamicTestBuilders(this IMethodSymbol methodSymbol)
    {
        var testAttribute = methodSymbol.GetRequiredTestAttribute();

        return new DynamicTestSourceDataModel
        {
            Class = methodSymbol.ContainingType,
            Method = methodSymbol,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
        };
    }
}
