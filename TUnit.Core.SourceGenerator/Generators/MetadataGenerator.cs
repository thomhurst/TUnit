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
        writer.AppendLine($"TestClassType = typeof({className}),");
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
            .Where(a => a.AttributeClass?.Name == "DependsOnAttribute")
            .Select(a => a.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList();

        if (dependsOnAttributes.Any())
        {
            writer.AppendLine($"DependsOn = new string[] {{ {string.Join(", ", dependsOnAttributes.Select(d => $"\"{d}\""))} }},");
        }
        else
        {
            writer.AppendLine("DependsOn = Array.Empty<string>(),");
        }
    }

    private void GenerateParameterTypes(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var parameters = testInfo.MethodSymbol.Parameters;
        
        if (!parameters.Any())
        {
            writer.AppendLine("ParameterTypes = Type.EmptyTypes,");
            return;
        }

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
    }

    private void GenerateDelegateReferences(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;

        writer.AppendLine($"InstanceFactory = TestDelegateStorage.GetInstanceFactory(\"{className}\"),");
        writer.AppendLine($"TestInvoker = TestDelegateStorage.GetTestInvoker(\"{className}.{methodName}\"),");
    }
}