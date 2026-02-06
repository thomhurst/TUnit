using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypedConstantParser
{
    private static readonly TypedConstantFormatter _formatter = new();

    public static string GetRawTypedConstantValue(TypedConstant typedConstant, ITypeSymbol? targetType = null)
    {
        // Use the formatter for consistent handling
        return _formatter.FormatForCode(typedConstant, targetType);
    }
}
