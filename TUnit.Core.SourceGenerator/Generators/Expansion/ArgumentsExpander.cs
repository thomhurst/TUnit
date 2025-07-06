using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

public sealed class ArgumentsExpander : ITestExpander
{
    public bool CanExpand(TestMethodMetadata testInfo)
    {
        return testInfo.MethodSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ArgumentsAttribute");
    }

    public int GenerateExpansions(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var argumentsAttributes = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        foreach (var attr in argumentsAttributes)
        {
            GenerateSingleArgumentsVariant(writer, testInfo, attr, variantIndex++);
        }

        return variantIndex;
    }

    private void GenerateSingleArgumentsVariant(CodeWriter writer, TestMethodMetadata testInfo, 
        AttributeData argumentsAttribute, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var argumentValues = ExtractArgumentValues(argumentsAttribute);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadataWithVariant(writer, testInfo, variantIndex, argumentValues);
        GenerateTestAttributes(writer, testInfo);
        
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateTypedDelegatesWithArguments(writer, testInfo, argumentValues);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private object?[] ExtractArgumentValues(AttributeData argumentsAttribute)
    {
        var args = argumentsAttribute.ConstructorArguments;
        
        if (args.IsDefaultOrEmpty)
        {
            return Array.Empty<object?>();
        }

        if (args.Length == 1 && args[0].Kind == TypedConstantKind.Array)
        {
            return args[0].Values.Select(v => ExtractTypedConstantValue(v)).ToArray();
        }
        
        return args.Select(arg => ExtractTypedConstantValue(arg)).ToArray();
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

    private void GenerateBasicMetadataWithVariant(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex, object?[] argumentValues)
    {
        var testIdArgs = string.Join(", ", argumentValues.Select(v => FormatArgumentForTestId(v)));
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
        writer.AppendLine($"// Embedded argument values: {string.Join(", ", argumentValues.Select(v => TypedConstantParser.FormatPrimitive(v)))}");
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

    private void GenerateTypedDelegatesWithArguments(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var isAsync = testInfo.MethodSymbol.IsAsync;

        writer.AppendLine($"InstanceFactory = args => new {className}(),");

        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var typedInstance = ({className})instance;");
        writer.AppendLine("// Using compile-time expanded arguments");
        
        var argList = string.Join(", ", argumentValues.Select(FormatArgumentValueWithCast));
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
        writer.AppendLine("// Using compile-time expanded arguments");
        
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

        GenerateExecutableTestFactory(writer, testInfo, argumentValues);
    }

    private void GenerateExecutableTestFactory(CodeWriter writer, TestMethodMetadata testInfo, object?[] argumentValues)
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
        
        var argsArray = string.Join(", ", argumentValues.Select(v => TypedConstantParser.FormatPrimitive(v)));
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

    private string FormatArgumentValueWithCast(object? value)
    {
        return TypedConstantParser.FormatPrimitive(value);
    }

    private static string FormatArgumentForTestId(object? value)
    {
        if (value == null)
            return "null";
        
        if (value is string str)
            return str;
        
        return value.ToString() ?? "null";
    }
}