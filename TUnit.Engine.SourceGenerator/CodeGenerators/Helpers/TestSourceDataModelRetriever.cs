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
        
        var classArgumentsContainers = ArgumentsRetriever.GetArguments(context, namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty, GetClassAttributes(namedTypeSymbol).Concat(namedTypeSymbol.ContainingAssembly.GetAttributes().Where(x => x.IsDataSourceAttribute())).ToImmutableArray(), namedTypeSymbol, ArgumentsType.ClassConstructor).ToArray();
        var testArgumentsContainers = ArgumentsRetriever.GetArguments(context, methodSymbol.Parameters, methodSymbol.GetAttributes(), namedTypeSymbol, ArgumentsType.Method);
        
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;

        foreach (var testArguments in testArgumentsContainers)
        {
            foreach (var classArguments in classArgumentsContainers)
            {
                foreach (var testSourceDataModel in GenerateTestSourceDataModels(methodSymbol, namedTypeSymbol, classArguments, runCount, testAttribute, testArguments))
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
        ArgumentsContainer classArguments, int runCount, AttributeData testAttribute, ArgumentsContainer testArguments)
    {
        if (classArguments is EmptyArgumentsContainer)
        {
            foreach (var testSourceDataModel in GenerateSingleClassInstance(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                         testArguments))
            {
                yield return testSourceDataModel;
            }

            yield break;
        }

        foreach (var generateMultipleClassInstance in GenerateMultipleClassInstances(methodSymbol, namedTypeSymbol, runCount, testAttribute,
                     classArguments, testArguments))
        {
            yield return generateMultipleClassInstance;
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        ArgumentsContainer testArguments)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                    MethodSymbol = methodSymbol,
                    ClassSymbol = namedTypeSymbol,
                    ClassArguments = new EmptyArgumentsContainer
                    {
                        ArgumentsType = ArgumentsType.ClassConstructor,
                        DisposeAfterTest = false,
                    },
                    TestArguments = testArguments,
                    CurrentRepeatAttempt = i,
                    TestAttribute = testAttribute,
                });
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, ArgumentsContainer classArguments,
        ArgumentsContainer testArguments)
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
        
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
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
        };
    }
}