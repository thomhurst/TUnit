using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Context used when building individual test definitions.
/// This is a subset of TestGenerationContext focused on what's needed for a single test.
/// </summary>
internal class TestDefinitionContext
{
    public required TestMetadataGenerationContext GenerationContext { get; init; }
    public required AttributeData? ClassDataAttribute { get; init; }
    public required AttributeData? MethodDataAttribute { get; init; }
    public required int TestIndex { get; init; }
    
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

        var testIndex = 0;

        // If no attributes, create one test with empty data providers
        if (!classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            yield return new TestDefinitionContext
            {
                GenerationContext = generationContext,
                ClassDataAttribute = null,
                MethodDataAttribute = null,
                TestIndex = testIndex
            };
            yield break;
        }

        // If we have class data but no method data
        if (classDataAttrs.Any() && !methodDataAttrs.Any())
        {
            foreach (var classAttr in classDataAttrs)
            {
                yield return new TestDefinitionContext
                {
                    GenerationContext = generationContext,
                    ClassDataAttribute = classAttr,
                    MethodDataAttribute = null,
                    TestIndex = testIndex++
                };
            }
        }
        // If we have method data but no class data
        else if (!classDataAttrs.Any() && methodDataAttrs.Any())
        {
            foreach (var methodAttr in methodDataAttrs)
            {
                yield return new TestDefinitionContext
                {
                    GenerationContext = generationContext,
                    ClassDataAttribute = null,
                    MethodDataAttribute = methodAttr,
                    TestIndex = testIndex++
                };
            }
        }
        // If we have both class and method data - create cartesian product
        else
        {
            foreach (var classAttr in classDataAttrs)
            {
                foreach (var methodAttr in methodDataAttrs)
                {
                    yield return new TestDefinitionContext
                    {
                        GenerationContext = generationContext,
                        ClassDataAttribute = classAttr,
                        MethodDataAttribute = methodAttr,
                        TestIndex = testIndex++
                    };
                }
            }
        }
    }
    
    private static bool IsCompileTimeDataSourceAttribute(AttributeData attr)
    {
        var attrName = attr.AttributeClass?.Name;
        
        // These can be handled at compile time through code generation:
        // - ArgumentsAttribute (direct data)
        // - MethodDataSourceAttribute (generate lambda to call method)
        // - Attributes inheriting from AsyncDataSourceGeneratorAttribute (generate lambda to instantiate and call)
        
        if (attrName is "ArgumentsAttribute" or "MethodDataSourceAttribute")
            return true;
            
        // Check if it inherits from AsyncDataSourceGeneratorAttribute
        var baseType = attr.AttributeClass?.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "AsyncDataSourceGeneratorAttribute")
                return true;
            baseType = baseType.BaseType;
        }
        
        return false;
    }
}