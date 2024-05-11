using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestInformationRetriever
{
    private const int DefaultOrder = int.MaxValue / 2;

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
        var notInParallelAttribute = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == "global::TUnit.Core.NotInParallelAttribute");
        
        return notInParallelAttribute?.NamedArguments.FirstOrDefault(x => x.Key == "Order").Value.Value as int? ?? DefaultOrder;
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

    public static string GetTestId(TestGenerationContext testGenerationContext)
    {
        var stringBuilder = new StringBuilder();
        if (testGenerationContext.ClassDataAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.ClassDataAttribute.AttributeClass!.ToDisplayString());
            
            if (testGenerationContext.ClassDataAttributeIndex != null)
            {
                stringBuilder.Append($":{testGenerationContext.ClassDataAttributeIndex}");
            }
            
            if (testGenerationContext.EnumerableClassMethodDataCurrentCount != null)
            {
                stringBuilder.Append($":{testGenerationContext.EnumerableClassMethodDataCurrentCount}");
            }

            stringBuilder.Append('.');
        }
        
        if (testGenerationContext.TestDataAttribute != null)
        {
            stringBuilder.Append(testGenerationContext.TestDataAttribute.AttributeClass!.ToDisplayString());
            
            if (testGenerationContext.TestDataAttributeIndex != null)
            {
                stringBuilder.Append($":{testGenerationContext.TestDataAttributeIndex}");
            }
            
            if (testGenerationContext.EnumerableTestMethodDataCurrentCount != null)
            {
                stringBuilder.Append($":{testGenerationContext.EnumerableTestMethodDataCurrentCount}");
            }
        }
        else
        {
            stringBuilder.Append(testGenerationContext.TestAttribute.AttributeClass!.ToDisplayString());
        }
        
        stringBuilder.Append('.');
        
        var fullyQualifiedClassName =
            testGenerationContext.ClassSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);

        stringBuilder.Append(fullyQualifiedClassName);
        stringBuilder.Append('.');
        
        var testName = testGenerationContext.MethodSymbol.Name;
        
        stringBuilder.Append(testName);
        stringBuilder.Append('.');
        
        var classParameters = testGenerationContext.ClassSymbol.Constructors.First().Parameters;
        
        var classParameterTypes = GetTypes(classParameters);
        
        stringBuilder.Append(classParameterTypes);

        var methodParameterTypes = GetTypes(testGenerationContext.MethodSymbol.Parameters);

        stringBuilder.Append(methodParameterTypes);

        stringBuilder.Append(':');
        
        stringBuilder.Append(testGenerationContext.RepeatCount);
        
        return stringBuilder.ToString();
    }

    private static string GetArgumentSourcePrefix(IEnumerable<Argument> testArguments)
    {
        if (testArguments.FirstOrDefault()?.ArgumentSource is { } argumentSource)
        {
            return $"{argumentSource}:";
        }
        
        return string.Empty;
    }

    public static string GetTypes(ImmutableArray<IParameterSymbol> parameters)
    {
        if (parameters.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var parameterTypesFullyQualified = parameters.Select(x => x.Type)
            .Select(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix));
        
        return $"({string.Join(",", parameterTypesFullyQualified)}).";
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