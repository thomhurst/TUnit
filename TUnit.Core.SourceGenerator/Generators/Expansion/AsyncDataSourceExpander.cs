using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class AsyncDataSourceExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "AsyncDataSourceGeneratorAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var asyncDataSourceAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "AsyncDataSourceGeneratorAttribute")
            .ToList();

        foreach (var attr in asyncDataSourceAttributes)
        {
            variantIndex = GenerateAsyncDataSourceVariants(writer, testInfo, attr, variantIndex);
        }

        return variantIndex;
    }

    private int GenerateAsyncDataSourceVariants(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData asyncDataSourceAttribute, int variantIndex)
    {
        // For async data sources, we generally can't resolve them at compile-time
        // Generate a runtime-expandable test that will be handled by the TestBuilder
        GenerateRuntimeExpandableAsyncTest(writer, testInfo, asyncDataSourceAttribute, variantIndex++);
        
        return variantIndex;
    }

    private void GenerateRuntimeExpandableAsyncTest(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData asyncDataSourceAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var dataSourceTypeName = ExtractDataSourceTypeName(asyncDataSourceAttribute);
        
        writer.AppendLine($"// Runtime-expandable async test for {dataSourceTypeName} - async data source");
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataForAsyncExpansion(writer, testInfo, variantIndex, dataSourceTypeName);
        GenerateTestAttributes(writer, testInfo);
        
        // For async runtime expansion, we'll use the existing data source infrastructure
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(), // Will be resolved at runtime");
        
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesForAsyncRuntimeExpansion(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private string ExtractDataSourceTypeName(AttributeData asyncDataSourceAttribute)
    {
        var args = asyncDataSourceAttribute.ConstructorArguments;
        
        if (args.Length > 0 && args[0].Value is ITypeSymbol typeSymbol)
        {
            return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        
        return "UnknownAsyncDataSource";
    }

    private void GenerateBasicMetadataForAsyncExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, string dataSourceTypeName)
    {
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}[{dataSourceTypeName}]\",");
        writer.AppendLine($"TestName = \"{testInfo.MethodSymbol.Name}\",");
        writer.AppendLine($"TestClassType = typeof({testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
        writer.AppendLine($"TestMethodName = \"{testInfo.MethodSymbol.Name}\",");
        var location = testInfo.MethodSymbol.Locations.FirstOrDefault();
        if (location != null && location.IsInSource)
        {
            var lineSpan = location.GetLineSpan();
            writer.AppendLine($"FilePath = @\"{lineSpan.Path?.Replace("\\", "\\\\")}\",");
            writer.AppendLine($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
        }
        else
        {
            writer.AppendLine("FilePath = null,");
            writer.AppendLine("LineNumber = 0,");
        }
        writer.AppendLine($"// Runtime-expandable from async {dataSourceTypeName}");
    }

    private void GenerateTestAttributes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("Categories = Array.Empty<string>(),");
        writer.AppendLine("IsSkipped = false,");
        writer.AppendLine("SkipReason = null,");
        writer.AppendLine("TimeoutMs = null,");
        writer.AppendLine("RetryCount = 0,");
        writer.AppendLine("CanRunInParallel = true,");
        writer.AppendLine("Dependencies = Array.Empty<TestDependency>(),");
        writer.AppendLine("AttributeFactory = null,");
    }

    private void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        writer.AppendLine("ParameterTypes = new Type[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"typeof({paramType}),");
        }
        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"\"{paramType}\",");
        }
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateEmptyHookMetadata(CodeWriter writer)
    {
        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("BeforeClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterClass = Array.Empty<HookMetadata>(),");
        writer.AppendLine("BeforeTest = Array.Empty<HookMetadata>(),");
        writer.AppendLine("AfterTest = Array.Empty<HookMetadata>()");
        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GenerateTypedDelegatesForAsyncRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        InstanceFactoryGenerator.GenerateInstanceFactory(writer, testInfo.TypeSymbol);
        writer.AppendLine("TestInvoker = null, // Will be set by TestBuilder for async runtime expansion");
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        writer.AppendLine("InvokeTypedTest = null, // Will be set by TestBuilder for async runtime expansion");
        writer.AppendLine("CreateExecutableTest = null, // Will be set by TestBuilder for async runtime expansion");
    }
}