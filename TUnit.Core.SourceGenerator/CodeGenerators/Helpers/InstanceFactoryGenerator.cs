using Microsoft.CodeAnalysis;

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
            var constructor = GetPrimaryConstructor(typeSymbol);
            if (constructor != null)
            {
                GenerateTypedConstructorCall(writer, className, constructor);
            }
            else
            {
                writer.AppendLine("InstanceFactory = null,");
            }
        }
        else
        {
            // For classes with default constructor, check for ALL required properties
            var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(typeSymbol);
            
            if (!requiredProperties.Any())
            {
                writer.AppendLine($"InstanceFactory = args => new {className}(),");
            }
            else
            {
                writer.AppendLine($"InstanceFactory = args => new {className}()");
                writer.AppendLine("{");
                writer.Indent();
                
                foreach (var property in requiredProperties)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"{property.Name} = {defaultValue},");
                }
                
                writer.Unindent();
                writer.AppendLine("},");
            }
        }
    }
    
    private static IMethodSymbol? GetPrimaryConstructor(ITypeSymbol typeSymbol)
    {
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();
        
        if (constructors.Count == 1)
        {
            return constructors[0];
        }
        
        var publicConstructors = constructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToList();
        return publicConstructors.Count == 1 ? publicConstructors[0] : publicConstructors.FirstOrDefault();
    }
    
    private static void GenerateTypedConstructorCall(CodeWriter writer, string className, IMethodSymbol constructor)
    {
        writer.AppendLine("InstanceFactory = args =>");
        writer.AppendLine("{");
        writer.Indent();
        
        // Check for required properties
        var requiredProperties = RequiredPropertyHelper.GetAllRequiredProperties(constructor.ContainingType);
        
        if (constructor.Parameters.Length == 0 && !requiredProperties.Any())
        {
            writer.AppendLine($"return new {className}();");
        }
        else
        {
            writer.Append($"return new {className}(");
            
            for (int i = 0; i < constructor.Parameters.Length; i++)
            {
                if (i > 0) writer.Append(", ");
                
                var param = constructor.Parameters[i];
                var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                writer.Append($"global::TUnit.Core.Helpers.CastHelper.Cast<{paramType}>(args[{i}])");
            }
            
            writer.Append(")");
            
            // Add object initializer for required properties
            if (requiredProperties.Any())
            {
                writer.AppendLine();
                writer.AppendLine("{");
                writer.Indent();
                
                foreach (var property in requiredProperties)
                {
                    var defaultValue = RequiredPropertyHelper.GetDefaultValueForType(property.Type);
                    writer.AppendLine($"{property.Name} = {defaultValue},");
                }
                
                writer.Unindent();
                writer.Append("}");
            }
            
            writer.AppendLine(";");
        }
        
        writer.Unindent();
        writer.AppendLine("},");
    }
}