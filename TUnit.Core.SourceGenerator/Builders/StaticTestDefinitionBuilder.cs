using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.DataProviders;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Builders;

/// <summary>
/// Builds StaticTestDefinition objects for AOT-compatible tests
/// </summary>
internal class StaticTestDefinitionBuilder : ITestDefinitionBuilder
{
    public bool CanBuild(TestMetadataGenerationContext context)
    {
        return context.CanUseStaticDefinition;
    }

    public void BuildTestDefinitions(CodeWriter writer, TestMetadataGenerationContext context)
    {
        writer.AppendLine("var testDescriptors = new System.Collections.Generic.List<ITestDescriptor>();");
        writer.AppendLine();

        GenerateStaticTestDefinitions(writer, context);

        writer.AppendLine();
        writer.AppendLine("TestSourceRegistrar.RegisterTests(testDescriptors);");
    }

    private void GenerateStaticTestDefinitions(CodeWriter writer, TestMetadataGenerationContext context)
    {
        var testInfo = context.TestInfo;
        
        // Get all data source attributes that can be handled at compile time
        var classDataAttrs = testInfo.TypeSymbol.GetAttributes()
            .Where(IsCompileTimeDataSourceAttribute)
            .ToList();
            
        var methodDataAttrs = testInfo.MethodSymbol.GetAttributes()
            .Where(IsCompileTimeDataSourceAttribute)
            .ToList();

        // If no attributes, create one test with empty data providers
        if (!classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            GenerateSingleStaticTestDefinition(writer, context, null, null, 0);
            return;
        }

        // Generate test definitions for each combination
        var testIndex = 0;
        
        // If we have class data but no method data
        if (classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            foreach (var classAttr in classDataAttrs)
            {
                GenerateSingleStaticTestDefinition(writer, context, classAttr, null, testIndex++);
            }
        }
        // If we have method data but no class data
        else if (!classDataAttrs.Any() && methodDataAttrs.Any())
        {
            foreach (var methodAttr in methodDataAttrs)
            {
                GenerateSingleStaticTestDefinition(writer, context, null, methodAttr, testIndex++);
            }
        }
        // If we have both class and method data - create cartesian product
        else
        {
            foreach (var classAttr in classDataAttrs)
            {
                foreach (var methodAttr in methodDataAttrs)
                {
                    GenerateSingleStaticTestDefinition(writer, context, classAttr, methodAttr, testIndex++);
                }
            }
        }
    }

    private void GenerateSingleStaticTestDefinition(
        CodeWriter writer,
        TestMetadataGenerationContext context,
        AttributeData? classArguments,
        AttributeData? methodArguments,
        int testIndex)
    {
        var testInfo = context.TestInfo;
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);

        using (writer.BeginObjectInitializer($"var staticDef_{testIndex} = new StaticTestDefinition"))
        {
            writer.AppendLine($"TestId = \"{context.ClassName}.{context.MethodName}_{testIndex}_{{{{TestIndex}}}}\",");
            writer.AppendLine($"DisplayName = \"{context.MethodName}\",");
            writer.AppendLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {testInfo.LineNumber},");
            writer.AppendLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
            writer.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
            writer.AppendLine($"SkipReason = {skipReason},");
            writer.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)},");
            writer.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
            writer.AppendLine($"TestClassType = typeof({context.ClassName}),");
            writer.AppendLine($"TestMethodInfo = typeof({context.ClassName}).GetMethod(\"{context.MethodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, {GenerateParameterTypesArray(testInfo.MethodSymbol)}, null)!,");

            // Generate class factory with CastHelper
            writer.AppendLine($"ClassFactory = {GenerateStaticClassFactory(context)},");

            // Generate method invoker with CastHelper
            writer.AppendLine($"MethodInvoker = {GenerateStaticMethodInvoker(testInfo.MethodSymbol, context.ClassName)},");

            // Generate property values provider
            writer.AppendLine($"PropertyValuesProvider = {GenerateStaticPropertyValuesProvider(testInfo.TypeSymbol)},");

            // Generate data providers
            writer.AppendLine($"ClassDataProvider = {DataProviderGeneratorFactory.GenerateDataProvider(classArguments, context, DataProviderType.ClassParameters)},");
            writer.AppendLine($"MethodDataProvider = {DataProviderGeneratorFactory.GenerateDataProvider(methodArguments, context, DataProviderType.TestParameters)}");
        }

        writer.AppendLine();
        writer.AppendLine($"testDescriptors.Add(staticDef_{testIndex});");
    }

    private static string GenerateStaticClassFactory(TestMetadataGenerationContext context)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("args => ");

        if (context.ConstructorWithParameters != null && !context.HasParameterlessConstructor)
        {
            writer.Append($"new {context.ClassName}(");
            var parameterList = string.Join(", ", context.ConstructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"TUnit.Core.Helpers.CastHelper.Cast<{typeName}>(args[{i}])";
                }));
            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            writer.Append($"new {context.ClassName}()");
        }

        // Add required properties initialization if any
        if (context.RequiredProperties.Any())
        {
            writer.Append(" { ");
            var propertyInitializers = context.RequiredProperties.Select(prop =>
            {
                var defaultValue = GetDefaultValueForType(prop.Type);
                return $"{prop.Name} = {defaultValue}";
            });
            writer.Append(string.Join(", ", propertyInitializers));
            writer.Append(" }");
        }

        return writer.ToString().Trim();
    }

    private static string GenerateStaticMethodInvoker(IMethodSymbol methodSymbol, string className)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        
        writer.Append("async (instance, args) => ");
        
        if (IsAsyncMethod(methodSymbol))
        {
            writer.Append("await ");
        }
        
        writer.Append($"(({className})instance).{methodSymbol.Name}(");
        
        // Generate method arguments with CastHelper
        var parameterList = string.Join(", ", methodSymbol.Parameters
            .Select((param, i) =>
            {
                var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                    .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                
                if (param.Type.Name == "CancellationToken")
                {
                    return "cancellationToken";
                }
                
                return $"TUnit.Core.Helpers.CastHelper.Cast<{typeName}>(args[{i}])";
            }));
        
        writer.Append(parameterList);
        writer.Append(")");
        
        return writer.ToString().Trim();
    }

    private static string GenerateStaticPropertyValuesProvider(INamedTypeSymbol typeSymbol)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        
        var propertiesWithDataSource = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ArgumentsAttribute" || 
                                                      attr.AttributeClass?.Name == "MethodDataSourceAttribute"))
            .ToList();

        if (!propertiesWithDataSource.Any())
        {
            writer.Append("() => new[] { new System.Collections.Generic.Dictionary<string, object?>() }");
        }
        else
        {
            // For properties with data sources, this will be handled at runtime
            // Return a placeholder that indicates runtime resolution is needed
            writer.Append("null!");
        }
        
        return writer.ToString().Trim();
    }

    private static bool IsCompileTimeDataSourceAttribute(AttributeData attr)
    {
        var attrName = attr.AttributeClass?.Name;
        
        // Only ArgumentsAttribute can be fully resolved at compile time
        return attrName == "ArgumentsAttribute";
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    private static string GenerateParameterTypesArray(IMethodSymbol method)
    {
        return GenerateParameterTypesArray(method.Parameters);
    }
    
    private static string GenerateParameterTypesArray(IEnumerable<IParameterSymbol> parameters)
    {
        var parameterList = parameters.ToList();
        
        if (!parameterList.Any())
        {
            return "System.Type.EmptyTypes";
        }
        
        // Check if any parameter contains type parameters
        if (parameterList.Any(p => ContainsTypeParameter(p.Type)))
        {
            // Return null to indicate that parameter type matching should be done at runtime
            return "null";
        }
        
        var parameterTypes = parameterList
            .Select(p => $"typeof({p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))})")
            .ToArray();
            
        return $"new System.Type[] {{ {string.Join(", ", parameterTypes)} }}";
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

    private static string GetDefaultValueForType(ITypeSymbol type)
    {
        return $"default({type.GloballyQualified()})";
    }
}