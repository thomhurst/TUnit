using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Generates strongly-typed property setter delegates eliminating boxing
/// </summary>
internal class TypedPropertySetterGenerator
{
    private readonly StringBuilder _stringBuilder = new();
    
    public string GeneratePropertySetters(List<TestMethodMetadata> testMethods, DiagnosticContext? diagnosticContext = null)
    {
        _stringBuilder.Clear();
        
        _stringBuilder.AppendLine("using System;");
        _stringBuilder.AppendLine("using System.Threading.Tasks;");
        _stringBuilder.AppendLine("using TUnit.Core;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("namespace TUnit.Generated;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("/// <summary>");
        _stringBuilder.AppendLine("/// Strongly-typed property setters for dependency injection");
        _stringBuilder.AppendLine("/// </summary>");
        _stringBuilder.AppendLine("internal static class TypedPropertySetters");
        _stringBuilder.AppendLine("{");
        
        var processedClasses = new HashSet<string>();
        
        // Group by class to generate property setters
        var classesByType = testMethods.GroupBy(t => t.TypeSymbol, SymbolEqualityComparer.Default);
        
        foreach (var classGroup in classesByType)
        {
            if (classGroup.Key is INamedTypeSymbol namedType)
            {
                var className = namedType.ToDisplayString();
                if (processedClasses.Contains(className))
                    continue;
                    
                processedClasses.Add(className);
                GeneratePropertySettersForClass(namedType, diagnosticContext);
            }
        }
        
        _stringBuilder.AppendLine("}");
        return _stringBuilder.ToString();
    }
    
    private void GeneratePropertySettersForClass(INamedTypeSymbol classSymbol, DiagnosticContext? diagnosticContext)
    {
        var className = classSymbol.ToDisplayString();
        var safeClassName = classSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        
        // Find injectable properties
        var injectableProperties = GetInjectableProperties(classSymbol);
        
        if (!injectableProperties.Any())
            return;
            
        _stringBuilder.AppendLine($"    #region Property Setters for {className}");
        _stringBuilder.AppendLine();
        
        foreach (var property in injectableProperties)
        {
            GenerateTypedPropertySetter(classSymbol, property);
        }
        
        // Generate bulk setter method
        GenerateBulkPropertySetter(classSymbol, injectableProperties);
        
        _stringBuilder.AppendLine("    #endregion");
        _stringBuilder.AppendLine();
    }
    
    private void GenerateTypedPropertySetter(INamedTypeSymbol classSymbol, IPropertySymbol property)
    {
        var className = classSymbol.ToDisplayString();
        var safeClassName = classSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        var propertyName = property.Name;
        var propertyType = property.Type.ToDisplayString();
        
        var setterName = $"{safeClassName}_{propertyName}_Setter";
        
        _stringBuilder.AppendLine($"    /// <summary>");
        _stringBuilder.AppendLine($"    /// Strongly-typed setter for {className}.{propertyName}");
        _stringBuilder.AppendLine($"    /// </summary>");
        _stringBuilder.AppendLine($"    public static readonly Action<{className}, {propertyType}> {setterName} = ");
        _stringBuilder.AppendLine($"        (instance, value) => instance.{propertyName} = value;");
        _stringBuilder.AppendLine();
        
        // Register with storage
        _stringBuilder.AppendLine($"    static {safeClassName}_{propertyName}_Setter()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine($"        TestDelegateStorage.RegisterPropertySetter(");
        _stringBuilder.AppendLine($"            \"{className}.{propertyName}\",");
        _stringBuilder.AppendLine($"            (instance, value) => {setterName}(({className})instance, ({propertyType})value!));");
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
    }
    
    private void GenerateBulkPropertySetter(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
    {
        var className = classSymbol.ToDisplayString();
        var safeClassName = classSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        
        _stringBuilder.AppendLine($"    /// <summary>");
        _stringBuilder.AppendLine($"    /// Bulk property setter for {className}");
        _stringBuilder.AppendLine($"    /// </summary>");
        _stringBuilder.AppendLine($"    public static void SetAllProperties_{safeClassName}({className} instance, IServiceProvider serviceProvider)");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine("        if (serviceProvider == null) return;");
        _stringBuilder.AppendLine();
        
        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var propertyType = property.Type.ToDisplayString();
            var isRequired = !property.Type.IsReferenceType || property.Type.NullableAnnotation != NullableAnnotation.Annotated;
            
            _stringBuilder.AppendLine($"        // Set {propertyName}");
            _stringBuilder.AppendLine($"        var {propertyName.ToLowerInvariant()} = serviceProvider.GetService(typeof({propertyType})) as {propertyType};");
            
            if (isRequired)
            {
                _stringBuilder.AppendLine($"        if ({propertyName.ToLowerInvariant()} == null)");
                _stringBuilder.AppendLine("        {");
                _stringBuilder.AppendLine($"            throw new InvalidOperationException(\"Required property '{propertyName}' of type '{propertyType}' could not be resolved.\");");
                _stringBuilder.AppendLine("        }");
            }
            
            _stringBuilder.AppendLine($"        if ({propertyName.ToLowerInvariant()} != null)");
            _stringBuilder.AppendLine("        {");
            _stringBuilder.AppendLine($"            {safeClassName}_{propertyName}_Setter(instance, {propertyName.ToLowerInvariant()});");
            _stringBuilder.AppendLine("        }");
            _stringBuilder.AppendLine();
        }
        
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
        
        // Register bulk setter
        _stringBuilder.AppendLine($"    static SetAllProperties_{safeClassName}()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine($"        TestDelegateStorage.RegisterBulkPropertySetter(");
        _stringBuilder.AppendLine($"            \"{className}\",");
        _stringBuilder.AppendLine($"            (instance, serviceProvider) => SetAllProperties_{safeClassName}(({className})instance, serviceProvider));");
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
    }
    
    private List<IPropertySymbol> GetInjectableProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<IPropertySymbol>();
        
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;
                
            // Check for injectable attributes or naming patterns
            var hasInjectAttribute = property.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "InjectAttribute" ||
                           attr.AttributeClass?.Name == "DataSourceAttribute");
                           
            var isAutoInjectable = property.Name.EndsWith("Container") || 
                                  property.Name.EndsWith("Service") ||
                                  property.Name.EndsWith("Repository");
                                  
            if (hasInjectAttribute || isAutoInjectable)
            {
                properties.Add(property);
            }
        }
        
        return properties;
    }
}