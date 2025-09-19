using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class TypedDataSourceOptimizer
{
    /// <summary>
    /// Determines if a typed data source can be optimized for the given parameter types
    /// </summary>
    public static bool CanOptimizeTypedDataSource(AttributeData dataSourceAttribute, IMethodSymbol testMethod)
    {
        if (!dataSourceAttribute.IsTypedDataSourceAttribute())
        {
            return false;
        }

        var dataSourceType = dataSourceAttribute.GetTypedDataSourceType();
        if (dataSourceType == null)
        {
            return false;
        }

        // For single parameter tests, check if types match directly
        if (testMethod.Parameters.Length == 1)
        {
            return SymbolEqualityComparer.Default.Equals(dataSourceType, testMethod.Parameters[0].Type);
        }
        
        // For multiple parameters, check if data source provides a matching tuple
        if (dataSourceType is INamedTypeSymbol { IsTupleType: true } namedType && 
            namedType.TupleElements.Length == testMethod.Parameters.Length)
        {
            for (var i = 0; i < testMethod.Parameters.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(namedType.TupleElements[i].Type, testMethod.Parameters[i].Type))
                {
                    return false;
                }
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Generates optimized code for accessing typed data source values
    /// </summary>
    public static void GenerateOptimizedDataSourceAccess(
        ICodeWriter writer, 
        AttributeData dataSourceAttribute,
        string dataSourceVariableName,
        string metadataVariableName,
        IMethodSymbol testMethod)
    {
        var dataSourceType = dataSourceAttribute.GetTypedDataSourceType();
        if (dataSourceType == null)
        {
            // Fallback to standard implementation
            GenerateStandardDataSourceAccess(writer, dataSourceVariableName, metadataVariableName);
            return;
        }
        
        var typedInterfaceName = $"global::TUnit.Core.ITypedDataSourceAttribute<{dataSourceType.GloballyQualified()}>";
        
        writer.AppendLine($"// Optimized typed data source access for {dataSourceType.Name}");
        writer.AppendLine($"var typedDataSource = ({typedInterfaceName}){dataSourceVariableName};");
        writer.AppendLine($"await foreach (var dataFunc in typedDataSource.GetTypedDataRowsAsync({metadataVariableName}))");
        writer.AppendLine("{");
        writer.Indent();
        
        if (testMethod.Parameters.Length == 1)
        {
            // Single parameter - direct assignment
            writer.AppendLine("var value = await dataFunc();");
            writer.AppendLine($"var args = new object?[] {{ value }};");
        }
        else if (dataSourceType is INamedTypeSymbol { IsTupleType: true } namedType)
        {
            // Tuple - decompose without boxing
            writer.AppendLine("var tuple = await dataFunc();");
            writer.Append("var args = new object?[] { ");
            for (var i = 0; i < namedType.TupleElements.Length; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }
                writer.Append($"tuple.Item{i + 1}");
            }
            writer.AppendLine(" };");
        }
        else
        {
            // Other types - use ToObjectArray if available
            writer.AppendLine("var value = await dataFunc();");
            writer.AppendLine("var args = value.ToObjectArray();");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
    }
    
    private static void GenerateStandardDataSourceAccess(
        ICodeWriter writer,
        string dataSourceVariableName,
        string metadataVariableName)
    {
        writer.AppendLine($"await foreach (var dataFunc in {dataSourceVariableName}.GetDataRowsAsync({metadataVariableName}))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var args = await dataFunc();");
        writer.Unindent();
        writer.AppendLine("}");
    }
}