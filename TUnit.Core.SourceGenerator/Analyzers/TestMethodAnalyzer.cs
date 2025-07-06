using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Analyzers;

/// <summary>
/// Analyzes test methods and extracts metadata.
/// </summary>
public class TestMethodAnalyzer : ITestAnalyzer
{
    private readonly IDataSourceAnalyzer _dataSourceAnalyzer;

    public TestMethodAnalyzer(IDataSourceAnalyzer dataSourceAnalyzer)
    {
        _dataSourceAnalyzer = dataSourceAnalyzer;
    }

    public TestMethodModel? AnalyzeMethod(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;

        // Skip abstract classes, static methods, and open generic types
        if (typeSymbol.IsAbstract || methodSymbol.IsStatic || (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length > 0))
        {
            return null;
        }

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        // Extract skip information
        var (isSkipped, skipReason) = ExtractSkipInfo(methodSymbol);

        // Extract timeout
        var timeout = ExtractTimeout(methodSymbol);

        // Extract repeat count
        var repeatCount = ExtractRepeatCount(methodSymbol);

        // Analyze data sources
        var methodDataSource = _dataSourceAnalyzer.AnalyzeMethodDataSource(methodSymbol);
        var classDataSource = _dataSourceAnalyzer.AnalyzeClassDataSource(typeSymbol);
        var propertyDataSources = _dataSourceAnalyzer.AnalyzePropertyDataSources(typeSymbol);

        return new TestMethodModel
        {
            TestId = $"{typeSymbol.ToDisplayString()}.{methodSymbol.Name}_{{{{TestIndex}}}}",
            DisplayName = methodSymbol.Name,
            FilePath = filePath,
            LineNumber = lineNumber,
            IsSkipped = isSkipped,
            SkipReason = skipReason,
            Timeout = timeout,
            RepeatCount = repeatCount,
            MethodSymbol = methodSymbol,
            ContainingType = typeSymbol,
            IsAsync = IsAsyncMethod(methodSymbol),
            MethodDataSource = methodDataSource,
            ClassDataSource = classDataSource,
            PropertyDataSources = propertyDataSources
        };
    }

    private static bool IsAsyncMethod(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }

    private static (bool isSkipped, string? skipReason) ExtractSkipInfo(IMethodSymbol methodSymbol)
    {
        var skipAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "SkipAttribute");

        if (skipAttribute != null)
        {
            var reason = skipAttribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
            return (true, string.IsNullOrEmpty(reason) ? null : $"\"{reason}\"");
        }

        return (false, null);
    }

    private static TimeSpan? ExtractTimeout(IMethodSymbol methodSymbol)
    {
        var timeoutAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TimeoutAttribute");

        if (timeoutAttribute != null && timeoutAttribute.ConstructorArguments.Length > 0)
        {
            if (timeoutAttribute.ConstructorArguments[0].Value is int milliseconds)
            {
                return TimeSpan.FromMilliseconds(milliseconds);
            }
        }

        return null;
    }

    private static int ExtractRepeatCount(IMethodSymbol methodSymbol)
    {
        var repeatAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");

        if (repeatAttribute != null && repeatAttribute.ConstructorArguments.Length > 0)
        {
            if (repeatAttribute.ConstructorArguments[0].Value is int count)
            {
                return count;
            }
        }

        return 1;
    }
}
