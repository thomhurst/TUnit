using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class TestInformationGenerator
{
    public static string GetNotInParallelConstraintKeys(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        var notInParallelAttributes = GetMethodAndClassAttributes(methodSymbol, namedTypeSymbol)
            .Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                        == "global::TUnit.Core.NotInParallelAttribute")
            .ToList();

        if (!notInParallelAttributes.Any())
        {
            return "null";
        }
        
        var notInConstraintKeys = notInParallelAttributes
            .SelectMany(x => x.ConstructorArguments)
            .SelectMany(x => x.Value is null ? x.Values.Select(x => x.Value) : [x.Value])
            .Select(x => $"\"{x}\"");
        
        return $"[{string.Join(", ", notInConstraintKeys)}]";
    }

    public static int GetRepeatCount(IMethodSymbol methodSymbol, INamedTypeSymbol classSymbol)
    {
        return GetMethodAndClassAttributes(methodSymbol, classSymbol)
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.RepeatAttribute")
            ?.ConstructorArguments.First().Value as int? ?? 0;
    }

    public static int GetRetryCount(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        return GetMethodAndClassAttributes(methodSymbol, namedTypeSymbol)
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.RetryAttribute")
            ?.ConstructorArguments.First().Value as int? ?? 0;
    }

    public static string GetTimeOut(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        var timeoutAttribute = GetMethodAndClassAttributes(methodSymbol, namedTypeSymbol)
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.TimeoutAttribute");

        if (timeoutAttribute is null)
        {
            return "null";
        }

        var timeoutMillis = (int)timeoutAttribute.ConstructorArguments.First().Value!;
        
        return $"global::System.TimeSpan.FromMilliseconds({timeoutMillis})";
    }

    public static IEnumerable<string> GetCategories(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        return GetMethodAndClassAttributes(methodSymbol, namedTypeSymbol)
            .Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                        == "global::TUnit.Core.TestCategoryAttribute")
            .Select(x => $"\"{x.ConstructorArguments.First().Value}\"");
    }

    public static IEnumerable<AttributeData> GetMethodAndClassAttributes(IMethodSymbol methodSymbol,
        INamedTypeSymbol classSymbol)
    {
        return [..methodSymbol.GetAttributes(), ..classSymbol.GetAttributes()];
    }

    public static string GetTestId(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, int classRepeatCount,
        int methodRepeatCount)
    {
        // Format must match TestDetails.GenerateUniqueId, but we can't share code
        // as we're inside a source generator
        var fullyQualifiedClassName =
            classSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        var testName = methodSymbol.Name;
        
        var classParameters = methodSymbol.ContainingType.Constructors.First().Parameters;
        
        var classParameterTypes = GetTypes(classParameters);

        var methodParameterTypes = GetTypes(methodSymbol.Parameters);
        
        return $"{fullyQualifiedClassName}.{testName}.{classParameterTypes}.{classRepeatCount}.{methodParameterTypes}.{methodRepeatCount}";
    }

    public static string GetTypes(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var parameterTypesFullyQualified = parameters.Select(x => x.Type)
            .Select(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix));
        
        return $"({string.Join(",", parameterTypesFullyQualified)})";
    }
}