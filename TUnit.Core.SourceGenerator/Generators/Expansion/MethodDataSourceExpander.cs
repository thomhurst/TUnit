using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class MethodDataSourceExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var methodDataSourceAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
            .ToList();

        foreach (var attr in methodDataSourceAttributes)
        {
            variantIndex = GenerateMethodDataSourceVariants(writer, testInfo, attr, variantIndex);
        }

        return variantIndex;
    }

    private int GenerateMethodDataSourceVariants(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData methodDataSourceAttribute, int variantIndex)
    {
        var dataSourceInfo = ExtractDataSourceInfo(methodDataSourceAttribute);
        
        // Try to resolve and evaluate the data source at compile-time
        var dataRows = TryResolveDataSourceAtCompileTime(testInfo, dataSourceInfo);
        
        if (dataRows != null)
        {
            // Generate individual test metadata for each data row
            foreach (var row in dataRows)
            {
                GenerateSingleDataRowVariant(writer, testInfo, row, variantIndex++, dataSourceInfo.MethodName);
            }
        }
        else
        {
            // Fallback: Generate single test that will expand at runtime
            // This is for complex data sources that can't be resolved at compile-time
            GenerateRuntimeExpandableTest(writer, testInfo, dataSourceInfo, variantIndex++);
        }

        return variantIndex;
    }

    private void GenerateSingleDataRowVariant(CodeWriter writer, TestMethodMetadata testInfo, 
        object?[] dataRow, int variantIndex, string dataSourceMethod)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataWithDataRow(writer, testInfo, variantIndex, dataRow, dataSourceMethod);
        GenerateTestAttributes(writer, testInfo);
        
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesWithDataRow(writer, testInfo, dataRow);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateRuntimeExpandableTest(CodeWriter writer, TestMethodMetadata testInfo, 
        DataSourceInfo dataSourceInfo, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"// Runtime-expandable test for {dataSourceInfo.MethodName} - complex data source");
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataForRuntimeExpansion(writer, testInfo, variantIndex, dataSourceInfo);
        GenerateTestAttributes(writer, testInfo);
        
        // For runtime expansion, we'll use the existing data source infrastructure
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(), // Will be resolved at runtime");
        
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesForRuntimeExpansion(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private DataSourceInfo ExtractDataSourceInfo(AttributeData methodDataSourceAttribute)
    {
        var args = methodDataSourceAttribute.ConstructorArguments;
        
        if (args.Length == 0)
            throw new InvalidOperationException("MethodDataSource requires method name");

        var methodName = args[0].Value?.ToString() ?? throw new InvalidOperationException("Invalid method name");
        
        // Check for Arguments property
        object?[]? arguments = null;
        var namedArgs = methodDataSourceAttribute.NamedArguments;
        foreach (var namedArg in namedArgs)
        {
            if (namedArg.Key == "Arguments" && namedArg.Value.Kind == TypedConstantKind.Array)
            {
                arguments = namedArg.Value.Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
                break;
            }
        }

        return new DataSourceInfo(methodName, arguments);
    }

    private object? ExtractTypedConstantValue(TypedConstant typedConstant)
    {
        return typedConstant.Kind switch
        {
            TypedConstantKind.Primitive => typedConstant.Value,
            TypedConstantKind.Enum => typedConstant.Value,
            TypedConstantKind.Type => typedConstant.Value,
            TypedConstantKind.Array => typedConstant.Values.Select(ExtractTypedConstantValue).ToArray(),
            _ => typedConstant.Value
        };
    }

    private object?[][]? TryResolveDataSourceAtCompileTime(TestMethodMetadata testInfo, DataSourceInfo dataSourceInfo)
    {
        // Try to find the data source method in the same class
        var dataSourceMethod = testInfo.TypeSymbol.GetMembers(dataSourceInfo.MethodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic);

        if (dataSourceMethod == null)
            return null;

        // For now, only handle very simple cases that return constant arrays
        // This could be expanded to handle more complex scenarios
        return TryExtractSimpleConstantData(dataSourceMethod, dataSourceInfo.Arguments);
    }

    private object?[][]? TryExtractSimpleConstantData(IMethodSymbol method, object?[]? arguments)
    {
        // This is a simplified implementation
        // In a real scenario, we'd need to analyze the method body to extract constant data
        // For now, return null to indicate we can't resolve at compile-time
        return null;
    }

    private void GenerateBasicMetadataWithDataRow(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, object?[] dataRow, string dataSourceMethod)
    {
        var testIdArgs = string.Join(", ", dataRow.Select(TypedConstantParser.FormatPrimitive));
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}({testIdArgs})\",");
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
        writer.AppendLine($"// Data from {dataSourceMethod}: {string.Join(", ", dataRow.Select(TypedConstantParser.FormatPrimitive))}");
    }

    private void GenerateBasicMetadataForRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo, 
        int variantIndex, DataSourceInfo dataSourceInfo)
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
        writer.AppendLine($"// Runtime-expandable from {dataSourceInfo.MethodName}");
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

    private void GenerateTypedDelegatesWithDataRow(CodeWriter writer, TestMethodMetadata testInfo, object?[] dataRow)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        writer.AppendLine($"InstanceFactory = args => new {className}(),");

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var typedInstance = ({className})instance;");
        writer.AppendLine("// Using compile-time expanded data row");
        
        var argList = string.Join(", ", dataRow.Select(TypedConstantParser.FormatPrimitive));
        if (isAsync)
        {
            writer.AppendLine($"await typedInstance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"typedInstance.{methodName}({argList});");
            writer.AppendLine("await Task.CompletedTask;");
        }
        
        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");

        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Using compile-time expanded data row");
        
        if (isAsync)
        {
            writer.AppendLine($"await instance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"instance.{methodName}({argList});");
        }
        
        writer.Unindent();
        writer.AppendLine("},");

        GenerateExecutableTestFactoryWithDataRow(writer, testInfo, dataRow);
    }

    private void GenerateTypedDelegatesForRuntimeExpansion(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        writer.AppendLine($"InstanceFactory = args => new {className}(),");
        writer.AppendLine("TestInvoker = null, // Will be set by TestBuilder for runtime expansion");
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        writer.AppendLine("InvokeTypedTest = null, // Will be set by TestBuilder for runtime expansion");
        writer.AppendLine("CreateExecutableTest = null, // Will be set by TestBuilder for runtime expansion");
    }

    private void GenerateExecutableTestFactoryWithDataRow(CodeWriter writer, TestMethodMetadata testInfo, object?[] dataRow)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine("CreateExecutableTest = (creationContext, metadata) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"return new global::TUnit.Engine.ExecutableTest<{className}>");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("TestId = creationContext.TestId,");
        writer.AppendLine("DisplayName = creationContext.DisplayName,");
        writer.AppendLine("Metadata = metadata,");
        
        var argsArray = string.Join(", ", dataRow.Select(TypedConstantParser.FormatPrimitive));
        writer.AppendLine($"Arguments = new object?[] {{ {argsArray} }},");
        
        writer.AppendLine("ClassArguments = creationContext.ClassArguments,");
        writer.AppendLine("PropertyValues = creationContext.PropertyValues,");
        writer.AppendLine("BeforeTestHooks = creationContext.BeforeTestHooks,");
        writer.AppendLine("AfterTestHooks = creationContext.AfterTestHooks,");
        writer.AppendLine("Context = creationContext.Context,");
        
        writer.AppendLine("CreateTypedInstance = async () =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (metadata.InstanceFactory != null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return await global::System.Threading.Tasks.Task.FromResult(metadata.InstanceFactory(creationContext.ClassArguments));");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine($"throw new global::System.InvalidOperationException(\"No instance factory for {className}\");");
        writer.Unindent();
        writer.AppendLine("},");
        
        writer.AppendLine("InvokeTypedTest = metadata.InvokeTypedTest ?? throw new global::System.InvalidOperationException(\"No test invoker\")");
        
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("},");
    }


    private record DataSourceInfo(string MethodName, object?[]? Arguments);
}