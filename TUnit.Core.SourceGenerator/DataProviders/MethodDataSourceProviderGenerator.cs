using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.DataProviders;

/// <summary>
/// Generates data providers for MethodDataSourceAttribute
/// </summary>
internal class MethodDataSourceProviderGenerator : IDataProviderGenerator
{
    public bool CanGenerate(AttributeData attribute)
    {
        return attribute.AttributeClass?.Name == "MethodDataSourceAttribute";
    }

    public string GenerateProvider(AttributeData attribute, TestMetadataGenerationContext context, DataProviderType providerType)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        
        // MethodDataSourceAttribute constructor: (Type? type, string methodName) or (string methodName)
        var args = attribute.ConstructorArguments;
        
        writer.Append("new TUnit.Core.MethodDataProvider(() => ");
        
        if (args.Length == 2 && args[0].Value != null)
        {
            // Type and method name
            var type = args[0].Value as ITypeSymbol;
            var methodName = args[1].Value?.ToString();
            
            // Find the method to check if it's static
            var method = type?.GetMembers(methodName!).OfType<IMethodSymbol>().FirstOrDefault();
            if (method != null && method.IsStatic)
            {
                writer.Append($"{type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}.{methodName}()");
            }
            else
            {
                // Instance method - create instance first
                writer.Append($"new {type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}().{methodName}()");
            }
        }
        else if (args.Length == 1 || (args.Length == 2 && args[0].Value == null))
        {
            // Just method name - refers to a method on the test class
            var methodName = args.Length == 1 ? args[0].Value?.ToString() : args[1].Value?.ToString();
            // For static definitions, we can't use instance methods without an instance
            // This is a limitation that should be handled by using DynamicTestMetadata instead
            writer.Append($"{context.ClassName}.{methodName}()");
        }
        
        writer.Append(")");
        return writer.ToString().Trim();
    }
}