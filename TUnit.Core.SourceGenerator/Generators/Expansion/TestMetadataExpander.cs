using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators.Expansion;

/// <summary>
/// Orchestrates test metadata expansion using various strategies.
/// Follows the Strategy pattern for clean separation of concerns.
/// </summary>
public sealed class TestMetadataExpander
{
    private readonly IReadOnlyList<ITestExpander> _expanders;

    public TestMetadataExpander()
    {
        _expanders = new List<ITestExpander>
        {
            new ArgumentsExpander(),
            new MethodDataSourceExpander(),
            new AsyncDataSourceExpander(),
            new AsyncUntypedDataSourceExpander()
        };
    }

    /// <summary>
    /// Generates expanded test metadata for all applicable test expansion strategies
    /// </summary>
    public void GenerateExpandedTestMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods without type arguments
        if (testInfo.MethodSymbol.IsGenericMethod &&
            (testInfo.GenericTypeArguments == null || testInfo.GenericTypeArguments.Length == 0))
        {
            writer.AppendLine($"// Skipped generic method {testInfo.MethodSymbol.Name} - no type arguments provided");
            return;
        }

        int variantIndex = 0;
        bool anyExpansions = false;

        // Apply each expansion strategy that can handle this test
        foreach (var expander in _expanders)
        {
            if (expander.CanExpand(testInfo))
            {
                variantIndex = expander.GenerateExpansions(writer, testInfo, variantIndex);
                anyExpansions = true;
            }
        }

        // If no expansions were applied, generate a single basic test metadata
        if (!anyExpansions)
        {
            GenerateSingleBasicTestMetadata(writer, testInfo, variantIndex);
        }
    }

    private void GenerateSingleBasicTestMetadata(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"_allTests.Add(new TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        GenerateBasicMetadata(writer, testInfo, variantIndex);
        GenerateTestAttributes(writer, testInfo);
        
        // No specific data sources - might have class/property data sources
        writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
        writer.AppendLine("PropertyDataSources = Array.Empty<PropertyDataSource>(),");
        
        GenerateParameterTypes(writer, testInfo);
        GenerateEmptyHookMetadata(writer);
        GenerateBasicTypedDelegates(writer, testInfo);

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateBasicMetadata(CodeWriter writer, TestMethodMetadata testInfo, int variantIndex)
    {
        writer.AppendLine($"TestId = \"{testInfo.TypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat)}.{testInfo.MethodSymbol.Name}\",");
        writer.AppendLine($"TestName = \"{testInfo.MethodSymbol.Name}\",");
        writer.AppendLine($"TestClassType = typeof({testInfo.TypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat)}),");
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
            var paramType = param.Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"typeof({paramType}),");
        }
        writer.Unindent();
        writer.AppendLine("},");

        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            var paramType = param.Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
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

    private void GenerateBasicTypedDelegates(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);

        writer.AppendLine($"InstanceFactory = args => new {className}(),");
        writer.AppendLine("TestInvoker = null, // Will be set by TestBuilder");
        writer.AppendLine($"PropertySetters = new Dictionary<string, Action<{className}, object?>>(),");
        writer.AppendLine("PropertyInjections = Array.Empty<PropertyInjectionData>(),");
        writer.AppendLine("CreateTypedInstance = null, // Will be set by TestBuilder");
        writer.AppendLine("InvokeTypedTest = null, // Will be set by TestBuilder");
        writer.AppendLine("CreateExecutableTest = null, // Will be set by TestBuilder");
    }
}