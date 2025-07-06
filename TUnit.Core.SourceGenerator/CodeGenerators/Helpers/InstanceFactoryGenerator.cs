using Microsoft.CodeAnalysis;
using System.Linq;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Centralized helper for generating instance factory code
/// </summary>
public static class InstanceFactoryGenerator
{
    /// <summary>
    /// Generates the instance factory code for a test class
    /// </summary>
    public static void GenerateInstanceFactory(CodeWriter writer, ITypeSymbol typeSymbol)
    {
        var className = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        if (typeSymbol.HasParameterizedConstructor())
        {
            // For classes with constructor parameters, leave InstanceFactory null
            // The TestBuilder will handle instance creation with proper constructor arguments
            writer.AppendLine("InstanceFactory = null,");
        }
        else
        {
            // For classes with default constructor, check for required properties
            var requiredPropertiesWithDataSource = RequiredPropertyHelper.GetRequiredPropertiesWithDataSource(typeSymbol);
            
            if (!requiredPropertiesWithDataSource.Any())
            {
                writer.AppendLine($"InstanceFactory = args => new {className}(),");
            }
            else
            {
                writer.AppendLine($"InstanceFactory = args => new {className}()");
                writer.AppendLine("{");
                writer.Indent();
                
                foreach (var property in requiredPropertiesWithDataSource)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"{property.Name} = {defaultValue},");
                }
                
                writer.Unindent();
                writer.AppendLine("},");
            }
        }
    }
}