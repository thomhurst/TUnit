using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TestSourceDataModelRetriever
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
        var classArgumentsContainers = ArgumentsRetriever.GetArguments(
                context,
                constructorParameters,
                null,
                constructorParameters.Select(x => x.Type).ToImmutableArray(),
                GetClassAttributes(namedTypeSymbol)
                    .Concat(namedTypeSymbol.ContainingAssembly.GetAttributes().Where(x => x.IsDataSourceAttribute()))
                    .ToImmutableArray(),
                namedTypeSymbol,
                methodSymbol,
                ArgumentsType.ClassConstructor)
            .ToArray();
        
        var methodParametersWithoutCancellationToken = methodSymbol.Parameters.WithoutCancellationTokenParameter();

        var testArgumentsContainers = ArgumentsRetriever.GetArguments(
            context,
            methodParametersWithoutCancellationToken,
            null,
            methodParametersWithoutCancellationToken.Select(x => x.Type).ToImmutableArray(),
            methodSymbol.GetAttributes(),
            namedTypeSymbol,
            methodSymbol,
            ArgumentsType.Method);
        
        var propertyArgumentsContainer = ArgumentsRetriever.GetProperties(context, namedTypeSymbol, methodSymbol);
        
        var repeatCount =
            TestInformationRetriever.GetRepeatCount(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));

        var runCount = repeatCount + 1;

        foreach (var testArguments in testArgumentsContainers)
        {
            foreach (var classArguments in classArgumentsContainers)
            {
                foreach (var testSourceDataModel in GenerateTestSourceDataModels(context, methodSymbol, namedTypeSymbol, classArguments, runCount, testAttribute, testArguments, propertyArgumentsContainer))
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

    private static IEnumerable<TestSourceDataModel> GenerateTestSourceDataModels(
        GeneratorAttributeSyntaxContext context, IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol,
        BaseContainer classArguments, int runCount, AttributeData testAttribute, BaseContainer testArguments,
        ClassPropertiesContainer classPropertiesContainer)
    {
        if (classArguments is EmptyArgumentsContainer)
        {
            foreach (var testSourceDataModel in GenerateSingleClassInstance(context, methodSymbol, namedTypeSymbol, runCount, testAttribute, testArguments, classPropertiesContainer))
            {
                yield return testSourceDataModel;
            }

            yield break;
        }

        foreach (var generateMultipleClassInstance in GenerateMultipleClassInstances(context, methodSymbol, namedTypeSymbol, runCount, testAttribute,
                     classArguments, testArguments, classPropertiesContainer))
        {
            yield return generateMultipleClassInstance;
        }
    }

    private static IEnumerable<TestSourceDataModel> GenerateSingleClassInstance(GeneratorAttributeSyntaxContext context,
        IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute,
        BaseContainer testArguments,
        ClassPropertiesContainer classPropertiesContainer)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                Context = context,
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

    private static IEnumerable<TestSourceDataModel> GenerateMultipleClassInstances(
        GeneratorAttributeSyntaxContext context, IMethodSymbol methodSymbol,
        INamedTypeSymbol namedTypeSymbol, int runCount, AttributeData testAttribute, BaseContainer classArguments,
        BaseContainer testArguments,
        ClassPropertiesContainer classPropertiesContainer)
    {
        for (var i = 0; i < runCount; i++)
        {
            yield return GetTestSourceDataModel(new TestGenerationContext
            {
                Context = context,
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

        var testAttributes = methodSymbol.GetAttributes().ExcludingSystemAttributes();
        var classAttributes = namedTypeSymbol.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes();
        var assemblyAttributes = namedTypeSymbol.ContainingAssembly.GetAttributes().ExcludingSystemAttributes();
        
        AttributeData[] allAttributes =
        [
            ..testAttributes,
            ..classAttributes,
            ..assemblyAttributes
        ];
        
        return new TestSourceDataModel
        {
            TestGenerationContext = testGenerationContext,
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName = namedTypeSymbol.GloballyQualified(),
            MinimalTypeName = namedTypeSymbol.Name,
            TestClass = namedTypeSymbol,
            TestMethod = methodSymbol,
            RepeatLimit = TestInformationRetriever.GetRepeatCount(allAttributes),
            CurrentRepeatAttempt = testGenerationContext.CurrentRepeatAttempt,
            ClassArguments = classArguments,
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
            MethodArgumentTypes = [..GetParameterTypes(methodSymbol, testArguments.GetArgumentTypes())],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            PropertyArguments = testGenerationContext.PropertyArguments,
            GenericSubstitutions = GetGenericSubstitutions(methodSymbol, testArguments.GetArgumentTypes())
        };
    }

    private static IDictionary<string, string>? GetGenericSubstitutions(IMethodSymbol methodSymbol, string[] argumentTypes)
    {
        if(methodSymbol.Parameters.Length is 0 || methodSymbol.TypeParameters.Length is 0)
        {
            return null;
        }

        var dictionary = new Dictionary<string, string>();
        
        for (var index = 0; index < methodSymbol.Parameters.Length; index++)
        {
            var parameter = methodSymbol.Parameters[index];

            if (parameter.Type.IsGenericDefinition())
            {
                dictionary[parameter.Type.GloballyQualified()] = argumentTypes[index];
            }
        }

        return dictionary;
    }

    private static IEnumerable<string> GetParameterTypes(IMethodSymbol methodSymbol, string[] argumentTypes)
    {
        for (var index = 0; index < methodSymbol.Parameters.Length; index++)
        {
            var parameter = methodSymbol.Parameters[index];

            if (parameter.Type.IsGenericDefinition())
            {
                yield return argumentTypes[index];
            }
            else
            {
                yield return parameter.Type.GloballyQualified();
            }
        }
    }
}