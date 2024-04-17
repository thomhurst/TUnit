using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestInformationRetriever
{
    public static string GetNotInParallelConstraintKeys(AttributeData[] methodAndClassAttributes)
    {
        var notInParallelAttributes = methodAndClassAttributes
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

    public static int GetRepeatCount(AttributeData[] methodAndClassAttributes)
    {
        return methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.RepeatAttribute")
            ?.ConstructorArguments.First().Value as int? ?? 0;
    }
    
    public static TestLocation GetTestLocation(AttributeData[] methodAndClassAttributes)
    {
        var testAttribute = methodAndClassAttributes
            .First(x => x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == WellKnownFullyQualifiedClassNames.BaseTestAttribute.WithGlobalPrefix);

        return new TestLocation
        {
            FilePath = TypedConstantParser.GetTypedConstantValue(testAttribute.ConstructorArguments[0]),
            LineNumber = int.Parse(TypedConstantParser.GetTypedConstantValue(testAttribute.ConstructorArguments[1]))
        };
    }

    public static int GetRetryCount(AttributeData[] methodAndClassAttributes)
    {
        var retryAttribute = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.RetryAttribute");
        
        return retryAttribute?.ConstructorArguments.First().Value as int? ?? 0;
    }

    public static int GetOrder(AttributeData[] methodAndClassAttributes)
    {
        var retryAttribute = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.OrderAttribute");
        
        return retryAttribute?.ConstructorArguments.First().Value as int? ?? int.MaxValue;
    }

    public static string GetTimeOut(AttributeData[] methodAndClassAttributes)
    {
        var timeoutAttribute = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.TimeoutAttribute");

        if (timeoutAttribute is null)
        {
            return "null";
        }

        var timeoutMillis = (int)timeoutAttribute.ConstructorArguments.First().Value!;
        
        return $"global::System.TimeSpan.FromMilliseconds({timeoutMillis})";
    }

    public static IEnumerable<string> GetCategories(AttributeData[] methodAndClassAttributes)
    {
        return methodAndClassAttributes
            .Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                        == "global::TUnit.Core.TestCategoryAttribute")
            .Select(x => $"\"{x.ConstructorArguments.First().Value}\"");
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

    public static string GetReturnType(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (returnType == "global::System.Void")
        {
            return "typeof(void)";
        }

        return $"typeof({returnType})";
    }
}