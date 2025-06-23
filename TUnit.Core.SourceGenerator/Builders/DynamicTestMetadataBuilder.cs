using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Builders;

/// <summary>
/// Builds DynamicTestMetadata objects for tests that require runtime resolution
/// </summary>
internal class DynamicTestMetadataBuilder : ITestDefinitionBuilder
{
    public bool CanBuild(TestMetadataGenerationContext context)
    {
        return !context.CanUseStaticDefinition;
    }

    public void BuildTestDefinitions(CodeWriter writer, TestMetadataGenerationContext context)
    {
        writer.AppendLine("var testMetadata = new System.Collections.Generic.List<DynamicTestMetadata>();");
        writer.AppendLine();

        GenerateDynamicTestMetadata(writer, context);

        writer.AppendLine();
        writer.AppendLine("TestSourceRegistrar.RegisterTests(testMetadata.Cast<ITestDescriptor>().ToList());");
    }

    private void GenerateDynamicTestMetadata(CodeWriter writer, TestMetadataGenerationContext context)
    {
        var testInfo = context.TestInfo;
        
        // Extract skip information
        var (isSkipped, skipReason) = CodeGenerationHelpers.ExtractSkipInfo(testInfo.MethodSymbol);

        // For generic types, we can't use typeof() so TestClassType will be null
        var testClassTypeValue = testInfo.TypeSymbol.IsGenericType ? "null" : $"typeof({context.ClassName})";

        // Create the test metadata object
        using (writer.BeginObjectInitializer("var metadata = new DynamicTestMetadata"))
        {
            writer.AppendLine($"TestIdTemplate = \"{context.ClassName}.{context.MethodName}_{{{{TestIndex}}}}\",");
            writer.AppendLine($"TestClassTypeReference = {CodeGenerationHelpers.GenerateTypeReference(testInfo.TypeSymbol)},");
            writer.AppendLine($"TestClassType = {testClassTypeValue},");

            writer.AppendLine($"MethodMetadata = {GenerateMethodMetadataUsingWriter(testInfo)},");
            writer.AppendLine($"TestFilePath = @\"{testInfo.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"TestLineNumber = {testInfo.LineNumber},");
            writer.AppendLine($"TestClassFactory = {GenerateTestClassFactory(context)},");
            writer.AppendLine($"ClassDataSources = {CodeGenerationHelpers.GenerateClassDataSourceProviders(testInfo.TypeSymbol)},");
            writer.AppendLine($"MethodDataSources = {CodeGenerationHelpers.GenerateMethodDataSourceProviders(testInfo.MethodSymbol)},");
            writer.AppendLine($"PropertyDataSources = {CodeGenerationHelpers.GeneratePropertyDataSourceDictionary(testInfo.TypeSymbol)},");
            writer.AppendLine($"DisplayNameTemplate = \"{context.MethodName}\",");
            writer.AppendLine($"RepeatCount = {CodeGenerationHelpers.ExtractRepeatCount(testInfo.MethodSymbol)},");
            writer.AppendLine($"IsAsync = {(IsAsyncMethod(testInfo.MethodSymbol) ? "true" : "false")},");
            writer.AppendLine($"IsSkipped = {(isSkipped ? "true" : "false")},");
            writer.AppendLine($"SkipReason = {skipReason},");
            writer.AppendLine($"Timeout = {CodeGenerationHelpers.ExtractTimeout(testInfo.MethodSymbol)}");
        }
        writer.AppendLine();
        writer.AppendLine("testMetadata.Add(metadata);");
    }

    private static string GenerateTestClassFactory(TestMetadataGenerationContext context)
    {
        var typeSymbol = context.TestInfo.TypeSymbol;
        
        // For generic types, we can't create instances at compile time
        if (typeSymbol.IsGenericType)
        {
            return "null!"; // Will be replaced at runtime by TestBuilder
        }

        // If there are any required properties, we need special handling
        if (context.RequiredProperties.Any())
        {
            return GenerateFactoryWithRequiredProperties(context);
        }

        // Simple factory without required properties
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("args => ");

        // If the class has a constructor with parameters and no parameterless constructor
        if (context.ConstructorWithParameters != null && !context.HasParameterlessConstructor)
        {
            // Use the args parameter which contains class constructor arguments
            writer.Append($"new {context.ClassName}(");

            // Generate argument list with proper type casting
            var parameterList = string.Join(", ", context.ConstructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));

            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            // Simple parameterless constructor
            writer.Append($"new {context.ClassName}()");
        }

        return writer.ToString().Trim();
    }

    private static string GenerateFactoryWithRequiredProperties(TestMetadataGenerationContext context)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        writer.Append("args => ");

        // Create a new instance with all required properties initialized
        if (context.ConstructorWithParameters != null && !context.HasParameterlessConstructor)
        {
            writer.Append($"new {context.ClassName}(");
            var parameterList = string.Join(", ", context.ConstructorWithParameters.Parameters
                .Select((param, i) =>
                {
                    var typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
                    return $"({typeName})args[{i}]";
                }));
            writer.Append(parameterList);
            writer.Append(")");
        }
        else
        {
            writer.Append($"new {context.ClassName}()");
        }

        // Always add object initializer for required properties
        writer.Append(" { ");
        var propertyInitializers = context.RequiredProperties.Select(prop =>
        {
            // For properties with data sources, create a minimal valid instance
            // that satisfies the compiler but will be replaced at runtime
            var defaultValue = GetDataSourceAwareDefaultValue(prop);
            return $"{prop.Name} = {defaultValue}";
        });
        writer.Append(string.Join(", ", propertyInitializers));
        writer.Append(" }");

        return writer.ToString().Trim();
    }

    private static string GetDataSourceAwareDefaultValue(IPropertySymbol property)
    {
        var type = property.Type;

        // For reference types, try to create a new instance
        if (type.IsReferenceType)
        {
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Special handling for common types
            if (type.SpecialType == SpecialType.System_String)
            {
                return "string.Empty";
            }

            // Check if the type has a parameterless constructor
            if (type is INamedTypeSymbol namedType)
            {
                var hasParameterlessConstructor = namedType.Constructors
                    .Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);

                if (hasParameterlessConstructor)
                {
                    return $"new {typeName}()";
                }
            }

            // Fallback to null with suppression
            return "null!";
        }

        // Use the existing logic for value types
        return GetDefaultValueForType(type);
    }

    private static string GetDefaultValueForType(ITypeSymbol type)
    {
        return $"default({type.GloballyQualified()})";
    }

    private static string GenerateMethodMetadataUsingWriter(TestMethodInfo testInfo)
    {
        using var writer = new CodeWriter("", includeHeader: false);
        
        // Use the existing writer to generate the metadata
        SourceInformationWriter.GenerateMethodInformation(writer, testInfo.Context, testInfo.TypeSymbol, testInfo.MethodSymbol, null, ',');
        
        // Remove the trailing comma and newline
        var result = writer.ToString().TrimEnd('\r', '\n', ',');
        return result;
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }
}