using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Context used when building individual test definitions.
/// This is a subset of TestGenerationContext focused on what's needed for a single test.
/// </summary>
public class TestDefinitionContext : IEquatable<TestDefinitionContext>
{
    public required TestMetadataGenerationContext GenerationContext { get; init; }
    public required AttributeData? ClassDataAttribute { get; init; }
    public required AttributeData? MethodDataAttribute { get; init; }
    public required int TestIndex { get; init; }
    public required int RepeatIndex { get; init; }

    /// <summary>
    /// Creates contexts for all test definitions based on data attributes
    /// </summary>
    public static IEnumerable<TestDefinitionContext> CreateContexts(TestMetadataGenerationContext generationContext)
    {
        var testInfo = generationContext.TestInfo;

        // Get all data source attributes that can be handled at compile time
        var classDataAttrs = testInfo.TypeSymbol.GetAttributes()
            .Where(attr => IsCompileTimeDataSourceAttribute(attr))
            .ToList();

        var methodDataAttrs = testInfo.MethodSymbol.GetAttributes()
            .Where(attr => IsCompileTimeDataSourceAttribute(attr))
            .ToList();

        // Extract repeat count
        var repeatCount = ExtractRepeatCount(testInfo.MethodSymbol);
        if (repeatCount == 0)
        {
            repeatCount = 1; // Default to 1 if no repeat attribute
        }

        var testIndex = 0;

        // If no attributes, create tests based on repeat count
        if (!classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
            {
                yield return new TestDefinitionContext
                {
                    GenerationContext = generationContext,
                    ClassDataAttribute = null,
                    MethodDataAttribute = null,
                    TestIndex = testIndex++,
                    RepeatIndex = repeatIndex
                };
            }
            yield break;
        }

        // If we have class data but no method data
        if (classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            foreach (var classAttr in classDataAttrs)
            {
                for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                {
                    yield return new TestDefinitionContext
                    {
                        GenerationContext = generationContext,
                        ClassDataAttribute = classAttr,
                        MethodDataAttribute = null,
                        TestIndex = testIndex++,
                        RepeatIndex = repeatIndex
                    };
                }
            }
        }
        // If we have method data but no class data
        else if (!classDataAttrs.Any() && methodDataAttrs.Any())
        {
            foreach (var methodAttr in methodDataAttrs)
            {
                for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                {
                    yield return new TestDefinitionContext
                    {
                        GenerationContext = generationContext,
                        ClassDataAttribute = null,
                        MethodDataAttribute = methodAttr,
                        TestIndex = testIndex++,
                        RepeatIndex = repeatIndex
                    };
                }
            }
        }
        // If we have both class and method data - create cartesian product
        else
        {
            foreach (var classAttr in classDataAttrs)
            {
                foreach (var methodAttr in methodDataAttrs)
                {
                    for (var repeatIndex = 0; repeatIndex < repeatCount; repeatIndex++)
                    {
                        yield return new TestDefinitionContext
                        {
                            GenerationContext = generationContext,
                            ClassDataAttribute = classAttr,
                            MethodDataAttribute = methodAttr,
                            TestIndex = testIndex++,
                            RepeatIndex = repeatIndex
                        };
                    }
                }
            }
        }
    }

    private static int ExtractRepeatCount(IMethodSymbol methodSymbol)
    {
        var repeatAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");

        if (repeatAttribute is { ConstructorArguments.Length: > 0 })
        {
            if (repeatAttribute.ConstructorArguments[0].Value is int count)
            {
                return count;
            }
        }

        return 0;
    }

    private static bool IsCompileTimeDataSourceAttribute(AttributeData attr)
    {
        var attrName = attr.AttributeClass?.Name;

        // These can be handled at compile time through code generation:
        // - ArgumentsAttribute (direct data)
        // - MethodDataSourceAttribute (generate lambda to call method)
        // - Attributes inheriting from AsyncDataSourceGeneratorAttribute (generate lambda to instantiate and call)

        if (attrName is "ArgumentsAttribute" or "MethodDataSourceAttribute")
        {
            return true;
        }

        // Check if it inherits from AsyncDataSourceGeneratorAttribute
        var baseType = attr.AttributeClass?.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AsyncDataSourceGeneratorAttribute")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    public bool Equals(TestDefinitionContext? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return GenerationContext.Equals(other.GenerationContext) &&
               AttributeDataEquals(ClassDataAttribute, other.ClassDataAttribute) &&
               AttributeDataEquals(MethodDataAttribute, other.MethodDataAttribute) &&
               TestIndex == other.TestIndex &&
               RepeatIndex == other.RepeatIndex;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestDefinitionContext);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = GenerationContext.GetHashCode();
            hashCode = (hashCode * 397) ^ AttributeDataGetHashCode(ClassDataAttribute);
            hashCode = (hashCode * 397) ^ AttributeDataGetHashCode(MethodDataAttribute);
            hashCode = (hashCode * 397) ^ TestIndex;
            hashCode = (hashCode * 397) ^ RepeatIndex;
            return hashCode;
        }
    }

    private static bool AttributeDataEquals(AttributeData? x, AttributeData? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        
        return SymbolEqualityComparer.Default.Equals(x.AttributeClass, y.AttributeClass) &&
               x.ConstructorArguments.Length == y.ConstructorArguments.Length &&
               x.ConstructorArguments.Zip(y.ConstructorArguments, (a, b) => TypedConstantEquals(a, b)).All(eq => eq);
    }

    private static bool TypedConstantEquals(TypedConstant x, TypedConstant y)
    {
        if (x.Kind != y.Kind) return false;
        if (!SymbolEqualityComparer.Default.Equals(x.Type, y.Type)) return false;
        return Equals(x.Value, y.Value);
    }

    private static int AttributeDataGetHashCode(AttributeData? attr)
    {
        if (attr is null) return 0;
        return SymbolEqualityComparer.Default.GetHashCode(attr.AttributeClass);
    }
}
