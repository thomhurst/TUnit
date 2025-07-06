using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class AsyncUntypedDataSourceExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "AsyncUntypedDataSourceGeneratorAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var untypedAsyncDataSourceAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "AsyncUntypedDataSourceGeneratorAttribute")
            .ToList();

        foreach (var attr in untypedAsyncDataSourceAttributes)
        {
            variantIndex = GenerateUntypedAsyncDataSourceVariants(writer, testInfo, attr, variantIndex);
        }

        return variantIndex;
    }

    private int GenerateUntypedAsyncDataSourceVariants(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData untypedAsyncDataSourceAttribute, int variantIndex)
    {
        // For untyped async data sources, we can't resolve them at compile-time
        // Generate a runtime-expandable test that will be handled by the TestBuilder
        GenerateRuntimeExpandableUntypedAsyncTest(writer, testInfo, untypedAsyncDataSourceAttribute, variantIndex++);
        
        return variantIndex;
    }

    private void GenerateRuntimeExpandableUntypedAsyncTest(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData untypedAsyncDataSourceAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var dataSourceInfo = ExtractUntypedDataSourceInfo(untypedAsyncDataSourceAttribute);
        
        writer.AppendLine($"// Runtime-expandable untyped async test for {dataSourceInfo.MethodName} - untyped async data source");
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataForUntypedAsyncExpansion(writer, testInfo, variantIndex, dataSourceInfo);
        GenerateTestAttributes(writer, testInfo);
        
        // For untyped async runtime expansion, we'll use the existing data source infrastructure
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(), // Will be resolved at runtime");
        
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesForUntypedAsyncRuntimeExpansion(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private UntypedDataSourceInfo ExtractUntypedDataSourceInfo(AttributeData untypedAsyncDataSourceAttribute)
    {
        var args = untypedAsyncDataSourceAttribute.ConstructorArguments;
        
        if (args.Length == 0)
            throw new InvalidOperationException("AsyncUntypedDataSourceGenerator requires method name");

        var methodName = args[0].Value?.ToString() ?? throw new InvalidOperationException("Invalid method name");
        
        // Check for Arguments property (similar to MethodDataSource)
        object?[]? arguments = null;
        var namedArgs = untypedAsyncDataSourceAttribute.NamedArguments;
        foreach (var namedArg in namedArgs)
        {
            if (namedArg.Key == "Arguments" && namedArg.Value.Kind == TypedConstantKind.Array)
            {
                arguments = namedArg.Value.Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
                break;
            }
        }

        return new UntypedDataSourceInfo(methodName, arguments);
    }

    private object? ExtractTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.IsNull)
        {
            return null;
        }
        
        return typedConstant.Kind switch
        {
            TypedConstantKind.Primitive => typedConstant.Value,
            TypedConstantKind.Enum => typedConstant.Value,
            TypedConstantKind.Type => typedConstant.Value,
            TypedConstantKind.Array => typedConstant.Values.Select(ExtractTypedConstantValue).ToArray(),
            _ => typedConstant.Value
        };
    }

    private void GenerateBasicMetadataForUntypedAsyncExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, UntypedDataSourceInfo dataSourceInfo)
    {
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}[{dataSourceInfo.MethodName}]\",");
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
        writer.AppendLine($"// Runtime-expandable from untyped async {dataSourceInfo.MethodName}");
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

    private void GenerateTypedDelegatesForUntypedAsyncRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        writer.AppendLine($"InstanceFactory = args => new {className}(),");
        writer.AppendLine("TestInvoker = null, // Will be set by TestBuilder for untyped async runtime expansion");
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        writer.AppendLine("InvokeTypedTest = null, // Will be set by TestBuilder for untyped async runtime expansion");
        writer.AppendLine("CreateExecutableTest = null, // Will be set by TestBuilder for untyped async runtime expansion");
    }

    private record UntypedDataSourceInfo(string MethodName, object?[]? Arguments);
}