using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating test metadata and registration
/// </summary>
internal sealed class MetadataGenerator
{
    private readonly HookGenerator _hookGenerator;
    private readonly DataSourceGenerator _dataSourceGenerator;

    public MetadataGenerator(HookGenerator hookGenerator, DataSourceGenerator dataSourceGenerator)
    {
        _hookGenerator = hookGenerator;
        _dataSourceGenerator = dataSourceGenerator;
    }

    /// <summary>
    /// Generates test metadata for all test methods
    /// </summary>
    public void GenerateTestRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var testCount = testMethods.Count();
        writer.AppendLine($"// Registering {testCount} tests");
        writer.AppendLine($"Console.Error.WriteLine(\"Registering {testCount} tests...\");");

        foreach (var testInfo in testMethods)
        {
            GenerateTestMetadata(writer, testInfo);
        }
    }

    private void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("_allTests.Add(new TestMetadata");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadata(writer, testInfo);
        GenerateTestAttributes(writer, testInfo);
        _dataSourceGenerator.GenerateDataSourceMetadata(writer, testInfo);
        GeneratePropertyDataSources(writer, testInfo);
        GenerateParameterTypes(writer, testInfo);
        _hookGenerator.GenerateHookMetadata(writer, testInfo);
        GenerateDelegateReferences(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateBasicMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var testId = $"{className}.{methodName}";
        
        if (testInfo.MethodSymbol.Parameters.Any())
        {
            var paramTypes = string.Join(",", testInfo.MethodSymbol.Parameters.Select(p => 
                p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            testId += $"({paramTypes})";
        }

        writer.AppendLine($"TestId = \"{testId}\",");
        writer.AppendLine($"TestName = \"{methodName}\",");
        
        // Check if the type contains unresolved type parameters
        if (ContainsTypeParameter(testInfo.TypeSymbol))
        {
            // This shouldn't happen with proper filtering, but provide a fallback
            writer.AppendLine($"TestClassType = typeof(object), // Generic type {className} cannot be resolved at compile time");
        }
        else
        {
            writer.AppendLine($"TestClassType = typeof({className}),");
        }
        
        writer.AppendLine($"TestMethodName = \"{methodName}\",");
        
        // File location if available
        var location = testInfo.MethodSymbol.Locations.FirstOrDefault();
        if (location != null && location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            writer.AppendLine($"FilePath = @\"{lineSpan.Path}\",");
            writer.AppendLine($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
        }
    }
    
    private static bool ContainsTypeParameter(ITypeSymbol type)
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

    private void GenerateTestAttributes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        GenerateCategories(writer, testInfo);
        GenerateSkipStatus(writer, testInfo);
        GenerateTimeout(writer, testInfo);
        GenerateRetryCount(writer, testInfo);
        GenerateParallelization(writer, testInfo);
        GenerateDependencies(writer, testInfo);
    }

    private void GenerateCategories(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var categories = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "CategoryAttribute")
            .Select(a => a.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        if (categories.Any())
        {
            writer.AppendLine($"Categories = new string[] {{ {string.Join(", ", categories.Select(c => $"\"{c}\""))} }},");
        }
        else
        {
            writer.AppendLine("Categories = Array.Empty<string>(),");
        }
    }

    private void GenerateSkipStatus(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var skipAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SkipAttribute");

        if (skipAttribute != null)
        {
            var reason = skipAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "No reason provided";
            writer.AppendLine("IsSkipped = true,");
            writer.AppendLine($"SkipReason = \"{reason}\",");
        }
        else
        {
            writer.AppendLine("IsSkipped = false,");
            writer.AppendLine("SkipReason = null,");
        }
    }

    private void GenerateTimeout(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var timeoutAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TimeoutAttribute");

        if (timeoutAttribute != null)
        {
            var timeout = timeoutAttribute.ConstructorArguments.FirstOrDefault().Value;
            writer.AppendLine($"TimeoutMs = {timeout},");
        }
        else
        {
            writer.AppendLine("TimeoutMs = null,");
        }
    }

    private void GenerateRetryCount(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var retryAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RetryAttribute");

        if (retryAttribute != null)
        {
            var retryCount = retryAttribute.ConstructorArguments.FirstOrDefault().Value ?? 0;
            writer.AppendLine($"RetryCount = {retryCount},");
        }
        else
        {
            writer.AppendLine("RetryCount = 0,");
        }
    }

    private void GenerateParallelization(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var notInParallelAttribute = testInfo.MethodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NotInParallelAttribute");

        writer.AppendLine($"CanRunInParallel = {(notInParallelAttribute == null).ToString().ToLower()},");
    }

    private void GenerateDependencies(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var dependsOnAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "DependsOnAttribute" || 
                       a.AttributeClass?.Name.StartsWith("DependsOnAttribute`") == true)
            .ToList();

        // Generate legacy DependsOn for backward compatibility
        var legacyDependencies = new List<string>();
        var testDependencies = new List<string>();

        foreach (var attr in dependsOnAttributes)
        {
            // Check if it's a generic DependsOnAttribute<T>
            if (attr.AttributeClass?.IsGenericType == true)
            {
                // Generic version: DependsOnAttribute<T>
                var typeArg = attr.AttributeClass.TypeArguments.FirstOrDefault();
                if (typeArg != null)
                {
                    var testName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                    if (string.IsNullOrEmpty(testName))
                    {
                        // Depends on all tests in the class
                        testDependencies.Add($"TestDependency.FromClass(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}))");
                    }
                    else
                    {
                        // Depends on specific test in the class
                        testDependencies.Add($"TestDependency.FromClassAndMethod(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), \"{testName}\")");
                    }
                }
            }
            else
            {
                // Non-generic version
                var args = attr.ConstructorArguments;
                if (args.Length == 1 && args[0].Type?.Name == "String")
                {
                    // DependsOnAttribute(string testName)
                    var testName = args[0].Value?.ToString();
                    if (!string.IsNullOrEmpty(testName))
                    {
                        legacyDependencies.Add(testName);
                        testDependencies.Add($"TestDependency.FromMethodName(\"{testName}\")");
                    }
                }
                else if (args.Length >= 1 && args[0].Type?.Name == "Type")
                {
                    // DependsOnAttribute(Type testClass) or DependsOnAttribute(Type testClass, string testName)
                    var classType = args[0].Value as ITypeSymbol;
                    if (classType != null)
                    {
                        var className = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (args.Length == 1)
                        {
                            // Depends on all tests in the class
                            testDependencies.Add($"TestDependency.FromClass(typeof({className}))");
                        }
                        else if (args.Length >= 2 && args[1].Type?.Name == "String")
                        {
                            var testName = args[1].Value?.ToString();
                            if (!string.IsNullOrEmpty(testName))
                            {
                                testDependencies.Add($"TestDependency.FromClassAndMethod(typeof({className}), \"{testName}\")");
                            }
                        }
                    }
                }
            }
        }

        // Generate DependsOn array (legacy)
        if (legacyDependencies.Any())
        {
            writer.AppendLine($"DependsOn = new string[] {{ {string.Join(", ", legacyDependencies.Select(d => $"\"{d}\""))} }},");
        }
        else
        {
            writer.AppendLine("DependsOn = Array.Empty<string>(),");
        }

        // Generate Dependencies array (new)
        if (testDependencies.Any())
        {
            writer.AppendLine("Dependencies = new TestDependency[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var dep in testDependencies)
            {
                writer.AppendLine($"{dep},");
            }
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("Dependencies = Array.Empty<TestDependency>(),");
        }
    }

    private void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var parameters = testInfo.MethodSymbol.Parameters;
        
        if (!parameters.Any())
        {
            writer.AppendLine("ParameterTypes = Type.EmptyTypes,");
            writer.AppendLine("TestMethodParameterTypes = Array.Empty<string>(),");
            return;
        }

        // Check if any parameter contains unresolved type parameters
        var hasUnresolvedTypeParameters = parameters.Any(p => DelegateGenerator.ContainsTypeParameter(p.Type));
        if (hasUnresolvedTypeParameters)
        {
            // For methods with unresolved type parameters, we can't generate typeof() at compile time
            writer.AppendLine("ParameterTypes = Type.EmptyTypes, // Method has unresolved type parameters");
            writer.AppendLine("TestMethodParameterTypes = Array.Empty<string>(), // Method has unresolved type parameters");
            return;
        }

        // Generate ParameterTypes
        writer.AppendLine("ParameterTypes = new Type[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in parameters)
        {
            var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"typeof({typeName}),");
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate TestMethodParameterTypes (string array for dependency matching)
        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var param in parameters)
        {
            var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"\"{typeName}\",");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateDelegateReferences(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;

        writer.AppendLine($"InstanceFactory = TestDelegateStorage.GetInstanceFactory(\"{className}\"),");
        writer.AppendLine($"TestInvoker = TestDelegateStorage.GetTestInvoker(\"{className}.{methodName}\"),");
    }

    private void GeneratePropertyDataSources(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var propertyDataSources = new List<(string name, ITypeSymbol type, AttributeData attr)>();
        
        // Find all properties with data source attributes
        foreach (var member in testInfo.TypeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Check for ClassDataSource attribute
            var classDataSourceAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "ClassDataSourceAttribute");
            if (classDataSourceAttr != null)
            {
                propertyDataSources.Add((member.Name, member.Type, classDataSourceAttr));
                continue;
            }
            
            // Check for MethodDataSource attribute on properties
            var methodDataSourceAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
            if (methodDataSourceAttr != null)
            {
                propertyDataSources.Add((member.Name, member.Type, methodDataSourceAttr));
                continue;
            }
            
            // Check for other data source attributes (e.g., custom attributes)
            var dataSourceAttrs = member.GetAttributes()
                .Where(a => a.AttributeClass?.AllInterfaces.Any(i => i.Name == "IDataAttribute") == true)
                .ToList();
            foreach (var attr in dataSourceAttrs)
            {
                propertyDataSources.Add((member.Name, member.Type, attr));
            }
        }

        if (!propertyDataSources.Any())
        {
            writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
            return;
        }

        writer.AppendLine("PropertyDataSources = new PropertyDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var (propName, propType, attr) in propertyDataSources)
        {
            writer.AppendLine("new PropertyDataSource");
            writer.AppendLine("{");
            writer.Indent();
            
            writer.AppendLine($"PropertyName = \"{propName}\",");
            writer.AppendLine($"PropertyType = typeof({propType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
            
            // Generate the appropriate data source based on attribute type
            GeneratePropertyDataSourceInstance(writer, attr, testInfo.TypeSymbol, propName);
            
            writer.Unindent();
            writer.AppendLine("},");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GeneratePropertyDataSourceInstance(CodeWriter writer, AttributeData attr, ITypeSymbol classType, string propertyName)
    {
        if (attr.AttributeClass?.Name == "ClassDataSourceAttribute")
        {
            // ClassDataSource typically creates a single instance
            var factoryKey = $"{classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{propertyName}_ClassDataSource";
            var sharedType = GetSharedTypeFromAttribute(attr);
            var isShared = sharedType != "None";
            
            writer.AppendLine($"DataSource = new DynamicTestDataSource({isShared.ToString().ToLower()}) {{ FactoryKey = \"{factoryKey}\" }}");
        }
        else if (attr.AttributeClass?.Name == "MethodDataSourceAttribute")
        {
            // MethodDataSource references a method
            var methodName = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (!string.IsNullOrEmpty(methodName))
            {
                var factoryKey = $"{classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{methodName}";
                writer.AppendLine($"DataSource = new DynamicTestDataSource(false) {{ FactoryKey = \"{factoryKey}\" }}");
            }
        }
        else
        {
            // For custom data attributes, use a generic approach
            var factoryKey = $"{classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{propertyName}_DataSource";
            writer.AppendLine($"DataSource = new DynamicTestDataSource(false) {{ FactoryKey = \"{factoryKey}\" }}");
        }
    }

    private string GetSharedTypeFromAttribute(AttributeData attr)
    {
        // Look for Shared property in named arguments
        var sharedArg = attr.NamedArguments.FirstOrDefault(na => na.Key == "Shared");
        if (sharedArg.Key != null)
        {
            var value = sharedArg.Value.Value?.ToString();
            return value ?? "None";
        }
        return "None";
    }
}