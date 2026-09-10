using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Formatting;

public interface ITypedConstantFormatter
{
    /// <summary>
    /// Formats a TypedConstant for use in generated C# code
    /// </summary>
    string FormatForCode(TypedConstant constant, ITypeSymbol? targetType = null);
    
    /// <summary>
    /// Formats a TypedConstant for use in test IDs (simplified, escaped)
    /// </summary>
    string FormatForTestId(TypedConstant constant);
    
    /// <summary>
    /// Formats a raw value for use in generated C# code
    /// </summary>
    string FormatValue(object? value, ITypeSymbol? targetType = null);
}