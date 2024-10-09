using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestSourceDataModelRetriever
{
    public static IEnumerable<TestSourceDataModel> ParseTestDatas(this IMethodSymbol methodSymbol,
        GeneratorAttributeSyntaxContext context,
        INamedTypeSymbol namedTypeSymbol)
    {
        if (methodSymbol.IsAbstract || namedTypeSymbol.IsAbstract || namedTypeSymbol.IsGenericType)
        {
            yield break;
        }
        
        var testAttribute = methodSymbol.GetRequiredTestAttribute();

        var constructorParameters = namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        var classArgumentsContainers = ArgumentsRetriever.GetArguments(context, constructorParameters, constructorParameters.Select(x => x.Type).ToImmutableArray(), GetClassAttributes(namedTypeSymbol).Concat(namedTypeSymbol.ContainingAssembly.GetAttributes().Where(x => x.IsDataSourceAttribute())).ToImmutableArray(), namedTypeSymbol, ArgumentsType.ClassConstructor).ToArray();
        var testArgumentsContainers = ArgumentsRetriever.GetArguments(context, methodSymbol.Parameters, methodSymbol.Parameters.Select(x => x.Type).ToImmutableArray(), methodSymbol.GetAttributes(), namedTypeSymbol, ArgumentsType.Method);
        var propertyArgumentsContainer = ArgumentsRetriever.GetProperties(context, namedTypeSymbol);
        
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;

        foreach (var testArguments in testArgumentsContainers)
        {
            foreach (var classArguments in classArgumentsContainers)
            {
                foreach (var testSourceDataModel in GenerateTestSourceDataModels(methodSymbol, namedTypeSymbol, classArguments, runCount, testAttribute, testArguments, propertyArgumentsContainer))
                {
                    yield return testSourceDataModel;
                }
            }
        }
    }

    private static IEnumerable<AttributeData> GetClassAttributes(INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.GetSelfAndBaseTypes().SelectMany(t => t.GetAttributes());
    }

    private static IEnumerable<TestSourceDataModel> GenerateTestSourceDataModels(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol,
        BaseContainer classArguments, int runCount, AttributeData testAttribute, BaseContainer testArguments, ClassPropertiesContainer classPropertiesContainer)
    {
        if (classArguments is EmptyArgumentsContainer)
        {
            foreach (var testSourceDataModel in GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute, testArguments, classPropertiesContainer))
            {
                yield return testSourceDataModel;
            }

            yield break;
        }

        foreach (var generateMultipleClassInstance in GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                     classArguments, testArguments, classPropertiesContainer))
        {
            yield return generateMultipleClassInstance;
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        BaseContainer testArguments,
        ClassPropertiesContainer classPropertiesContainer)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = new EmptyArgumentsContainer(),
                    TestArguments = testArguments,
                    CurrentRepeatAttempt = i,
                    TestAttribute = testAttribute,
                    PropertyArguments = classPropertiesContainer
                });
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, BaseContainer classArguments,
        BaseContainer testArguments,
        ClassPropertiesContainer classPropertiesContainer)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                MethodSymbol = methodSymbol,
                ClassSymbol = namedTypeSymbol,
                ClassArguments = classArguments,
                TestArguments = testArguments,
                CurrentRepeatAttempt = i,
                TestAttribute = testAttribute,
                PropertyArguments = classPropertiesContainer
            });
        }
    }

    private static TestSourceDataModel GetTestSourceDataModel(TestGenerationContext testGenerationContext)
    {
        var methodSymbol = testGenerationContext.MethodSymbol;
        var namedTypeSymbol = testGenerationContext.ClassSymbol;
        var classArguments = testGenerationContext.ClassArguments;
        var testArguments = testGenerationContext.TestArguments;
        var testAttribute = testGenerationContext.TestAttribute;

        AttributeData[] allAttributes =
        [
            ..methodSymbol.GetAttributes(),
            ..namedTypeSymbol.GetAttributesIncludingBaseTypes(),
            ..namedTypeSymbol.ContainingAssembly.GetAttributes()
        ];

        var propertyAttributes = testGenerationContext.PropertyArguments
            .InnerContainers
            .Select(x => x.PropertySymbol)
            .SelectMany(x => x.GetAttributes())
            .Where(x => x.IsDataSourceAttribute());
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            MinimalTypeName = namedTypeSymbol.Name,
            RepeatLimit = TestInformationRetriever.GetRepeatCount(allAttributes),
            CurrentRepeatAttempt = testGenerationContext.CurrentRepeatAttempt,
            ClassArguments = classArguments,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
            MethodParameterTypes = [..methodSymbol.Parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))],
            MethodParameterNames = [..methodSymbol.Parameters.Select(x => x.Name)],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            CustomDisplayName = allAttributes.FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == WellKnownFullyQualifiedClassNames.DisplayNameAttribute.WithGlobalPrefix)?.ConstructorArguments.First().Value as string,
            HasTimeoutAttribute = allAttributes.Any(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.TimeoutAttribute.WithGlobalPrefix) == true),
            TestExecutor = allAttributes.FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::TUnit.Core.Executors.TestExecutorAttribute") == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ParallelLimit = allAttributes.FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::TUnit.Core.ParallelLimiterAttribute") == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            AttributeTypes = allAttributes.Select(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).OfType<string>().Distinct().ToArray(),
            PropertyAttributeTypes = propertyAttributes.Select(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)).OfType<string>().ToArray(),
            PropertyArguments = testGenerationContext.PropertyArguments,
        };
    }
}