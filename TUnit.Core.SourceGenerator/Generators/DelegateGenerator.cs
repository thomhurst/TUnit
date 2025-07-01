using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating strongly-typed delegates for test execution
/// </summary>
internal sealed class DelegateGenerator
{
    /// <summary>
    /// Generates delegate registration code for all test methods
    /// </summary>
    public void GenerateDelegateRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("// Registering delegates for AOT execution");
        
        foreach (var testInfo in testMethods)
        {
            // Skip generic methods - they need special handling
            if (testInfo.MethodSymbol.IsGenericMethod)
                continue;
                
            GenerateInstanceFactoryRegistration(writer, testInfo);
            GenerateTestInvokerRegistration(writer, testInfo);
        }
    }

    /// <summary>
    /// Generates strongly-typed delegate definitions inline
    /// </summary>
    public void GenerateStronglyTypedDelegates(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var processedMethods = new HashSet<string>();
        
        foreach (var testInfo in testMethods)
        {
            var delegateName = GetTestDelegateName(testInfo);
            
            if (processedMethods.Add(delegateName))
            {
                GenerateTestDelegate(writer, testInfo);
            }
        }
    }

    private void GenerateInstanceFactoryRegistration(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeFactoryName = GetSafeFactoryMethodName(testInfo.TypeSymbol);
        
        // Safety check: don't generate if type contains type parameters
        if (ContainsTypeParameter(testInfo.TypeSymbol))
        {
            writer.AppendLine($"// Skipped instance factory for {className} - contains unresolved type parameters");
            return;
        }
        
        // Check if the class has required properties with data source attributes
        var hasRequiredPropertiesWithDataSource = HasRequiredPropertiesWithDataSource(testInfo.TypeSymbol);
        
        if (hasRequiredPropertiesWithDataSource)
        {
            // For classes with required properties that will be populated by data sources,
            // we need to generate a factory that provides default values
            writer.AppendLine($"TestDelegateStorage.RegisterInstanceFactory(\"{className}\", {safeFactoryName}_Factory);");
        }
        else
        {
            // Standard instantiation
            writer.AppendLine($"TestDelegateStorage.RegisterInstanceFactory(\"{className}\", args => new {className}({GenerateConstructorArgs(testInfo)}));");
        }
    }

    private void GenerateTestInvokerRegistration(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var invokerName = GetTestDelegateName(testInfo);
        
        writer.AppendLine($"TestDelegateStorage.RegisterTestInvoker(\"{className}.{methodName}\", {invokerName});");
    }

    private void GenerateTestDelegate(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods - they need special handling
        if (testInfo.MethodSymbol.IsGenericMethod)
        {
            return;
        }

        var delegateName = GetTestDelegateName(testInfo);
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        
        writer.AppendLine($"private static async Task {delegateName}(object instance, object?[] args)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var typedInstance = ({className})instance;");
        
        // Generate parameter unpacking
        var parameters = testInfo.MethodSymbol.Parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
        }
        
        // Generate method invocation
        var argList = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(i => $"arg{i}"));
        
        if (testInfo.MethodSymbol.IsAsync)
        {
            writer.AppendLine($"await typedInstance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"typedInstance.{methodName}({argList});");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private string GetTestDelegateName(TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.Name.Replace(".", "_");
        var methodName = testInfo.MethodSymbol.Name;
        return $"{className}_{methodName}_Invoker";
    }

    private string GenerateConstructorArgs(TestMethodMetadata testInfo)
    {
        var constructors = testInfo.TypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (!constructors.Any() || constructors.All(c => c.Parameters.Length == 0))
        {
            return string.Empty;
        }

        // Find the constructor with parameters and generate cast arguments
        var constructor = constructors.FirstOrDefault(c => c.Parameters.Length > 0);
        if (constructor == null)
        {
            return string.Empty;
        }

        var argList = new List<string>();
        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            
            // Check if the parameter type contains type parameters
            if (ContainsTypeParameter(param.Type))
            {
                // Skip generation for types with unresolved type parameters
                // This shouldn't happen with proper filtering, but acts as a safety net
                return string.Empty;
            }
            
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            argList.Add($"({paramType})args[{i}]");
        }

        return string.Join(", ", argList);
    }
    
    internal static bool ContainsTypeParameter(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsTypeParameter(arrayType.ElementType);
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ContainsTypeParameter);
        }

        return false;
    }
    
    private bool HasRequiredPropertiesWithDataSource(ITypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Check if it's a required property
            if (!member.IsRequired)
                continue;
                
            // Check if it has any data source attributes
            var hasDataSourceAttr = member.GetAttributes().Any(a => 
                a.AttributeClass?.Name == "ClassDataSourceAttribute" ||
                a.AttributeClass?.Name == "MethodDataSourceAttribute" ||
                a.AttributeClass?.Name == "DataSourceForAttribute" ||
                a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true);
                
            if (hasDataSourceAttr)
                return true;
        }
        
        return false;
    }
    
    private string GetSafeFactoryMethodName(ITypeSymbol typeSymbol)
    {
        // Use the full namespace and class name to ensure uniqueness
        var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        // Remove global:: prefix and replace invalid characters
        var safeName = fullName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("::", "_");
            
        return safeName;
    }
    
    /// <summary>
    /// Generates factory methods for classes with required properties
    /// </summary>
    public void GenerateRequiredPropertyFactories(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var processedTypes = new HashSet<string>();
        
        foreach (var testInfo in testMethods)
        {
            if (!HasRequiredPropertiesWithDataSource(testInfo.TypeSymbol))
                continue;
                
            var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var safeFactoryName = GetSafeFactoryMethodName(testInfo.TypeSymbol);
            
            if (!processedTypes.Add(className))
                continue;
                
            writer.AppendLine($"private static object {safeFactoryName}_Factory(object?[] args)");
            writer.AppendLine("{");
            writer.Indent();
            
            // Generate constructor arguments
            var ctorArgs = GenerateConstructorArgs(testInfo);
            
            writer.AppendLine($"return new {className}({ctorArgs})");
            writer.AppendLine("{");
            writer.Indent();
            
            // Initialize required properties with default values
            foreach (var member in testInfo.TypeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (!member.IsRequired)
                    continue;
                    
                var hasDataSourceAttr = member.GetAttributes().Any(a => 
                    a.AttributeClass?.Name == "ClassDataSourceAttribute" ||
                    a.AttributeClass?.Name == "MethodDataSourceAttribute" ||
                    a.AttributeClass?.Name == "DataSourceForAttribute" ||
                    a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true);
                    
                if (hasDataSourceAttr)
                {
                    // Provide a default value - the runtime will replace it with the actual value
                    writer.AppendLine($"{member.Name} = default!,");
                }
            }
            
            writer.Unindent();
            writer.AppendLine("};");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
        }
    }
}