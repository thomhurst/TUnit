using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var classArgumentsContainers = ArgumentsRetriever.GetArguments(context, constructorParameters, constructorParameters.Select(x => x.Type).ToImmutableArray(), GetClassAttributes(namedTypeSymbol).Concat(namedTypeSymbol.ContainingAssembly.GetAttributes().Where(x => x.IsDataSourceAttribute())).ToImmutableArray(), namedTypeSymbol, ArgumentsType.ClassConstructor).ToArray();
        var methodParametersWithoutCancellationToken = methodSymbol.Parameters.WithoutCancellationTokenParameter();
        var testArgumentsContainers = ArgumentsRetriever.GetArguments(context, methodParametersWithoutCancellationToken, methodParametersWithoutCancellationToken.Select(x => x.Type).ToImmutableArray(), methodSymbol.GetAttributes(), namedTypeSymbol, ArgumentsType.Method);
        var propertyArgumentsContainer = ArgumentsRetriever.GetProperties(context, namedTypeSymbol);
        
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

        var testAttributes = methodSymbol.GetAttributes();
        var classAttributes = namedTypeSymbol.GetAttributesIncludingBaseTypes().ToImmutableArray();
        var assemblyAttributes = namedTypeSymbol.ContainingAssembly.GetAttributes();
        
        AttributeData[] allAttributes =
        [
            ..testAttributes.Where(x => x.AttributeClass?.ContainingAssembly.Name != "System.Runtime"),
            ..classAttributes.Where(x => x.AttributeClass?.ContainingAssembly.Name != "System.Runtime"),
            ..assemblyAttributes.Where(x => x.AttributeClass?.ContainingAssembly.Name != "System.Runtime")
        ];

        var propertyAttributes = testGenerationContext.PropertyArguments
            .InnerContainers
            .Select(x => x.PropertySymbol)
            .SelectMany(x => x.GetAttributes())
            .Where(x => x.IsDataSourceAttribute());

        var methodNonGenericTypes = GetNonGenericTypes(testGenerationContext.MethodSymbol.Parameters,
            testArguments.GetArgumentTypes());

        var classNonGenericTypes =
            GetNonGenericTypes(
                testGenerationContext.ClassSymbol.InstanceConstructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty,
                classArguments.GetArgumentTypes());
        
        return new TestSourceDataModel
        {
            TestId = TestInformationRetriever.GetTestId(testGenerationContext),
            MethodName = methodSymbol.Name,
            FullyQualifiedTypeName = namedTypeSymbol.GloballyQualified(),
            MinimalTypeName = namedTypeSymbol.Name,
            AssemblyName = namedTypeSymbol.ContainingAssembly.Name,
            Namespace = namedTypeSymbol.ContainingNamespace.Name,
            RepeatLimit = TestInformationRetriever.GetRepeatCount(allAttributes),
            CurrentRepeatAttempt = testGenerationContext.CurrentRepeatAttempt,
            ClassArguments = classArguments,
            ClassParameterOrArgumentNonGenericTypes = classNonGenericTypes.ToArray(),
            MethodArguments = testArguments,
            FilePath = testAttribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty,
            LineNumber = testAttribute.ConstructorArguments[1].Value as int? ?? 0,
            MethodParameterTypes = [..methodSymbol.Parameters.Select(x => x.Type.GloballyQualified())],
            MethodParameterOrArgumentNonGenericTypes = methodNonGenericTypes.ToArray(),
            MethodParameterNames = [..methodSymbol.Parameters.Select(x => x.Name)],
            MethodGenericTypeCount = methodSymbol.TypeParameters.Length,
            TestExecutor = allAttributes.FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::TUnit.Core.Executors.TestExecutorAttribute") == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.GloballyQualified(),
            TestAttributes = WriteAttributes(testGenerationContext.Context, testAttributes),
            ClassAttributes = WriteAttributes(testGenerationContext.Context, classAttributes),
            AssemblyAttributes = WriteAttributes(testGenerationContext.Context, assemblyAttributes),
            PropertyAttributeTypes = propertyAttributes.Select(x => x.AttributeClass?.GloballyQualified()).OfType<string>().ToArray(),
            PropertyArguments = testGenerationContext.PropertyArguments,
        };
    }
    
    private static string[] WriteAttributes(GeneratorAttributeSyntaxContext context, ImmutableArray<AttributeData> attributeDatas)
    {
        return attributeDatas
            .Select(x => WriteAttribute(context, x))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

    private static string WriteAttribute(GeneratorAttributeSyntaxContext context, AttributeData attributeData)
    {
        if (attributeData.ApplicationSyntaxReference is null)
        {
            return string.Empty;
        }

        var attributeSyntax = attributeData.ApplicationSyntaxReference.GetSyntax();

        var constructorArgumentSyntaxes = attributeSyntax.DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .Where(x => x.NameEquals is null);

        var typedConstantsToExpression =
            constructorArgumentSyntaxes.Zip(attributeData.ConstructorArguments, (syntax, constant) => (syntax, constant));
        
        var constructorArguments = typedConstantsToExpression.Select(x =>
            TypedConstantParser.GetTypedConstantValue(context.SemanticModel, x.syntax.Expression, x.constant.Type));

        var namedArgSyntaxes = attributeSyntax.DescendantNodes()
            .OfType<AttributeArgumentSyntax>()
            .Where(x => x.NameEquals is not null)
            .ToArray();

        var namedArguments = attributeData.NamedArguments.Select(x =>
            $"{x.Key} = {TypedConstantParser.GetTypedConstantValue(context.SemanticModel, namedArgSyntaxes.First(stx => stx.NameEquals?.Name.Identifier.ValueText == x.Key).Expression, x.Value.Type)},");

        return $$"""
                new {{attributeData.AttributeClass!.GloballyQualified()}}({{string.Join(", ", constructorArguments)}})
                {
                    {{string.Join(" ", namedArguments)}}
                }
                """;
    }

    private static IEnumerable<string> GetNonGenericTypes(ImmutableArray<IParameterSymbol> methodSymbolParameters,
        string[] argumentTypes)
    {
        for (var i = 0; i < methodSymbolParameters.Length; i++)
        {
            var parameter = methodSymbolParameters[i];

            if (parameter.Type.IsGenericDefinition())
            {
                yield return argumentTypes.ElementAtOrDefault(i) ?? "global::System.Threading.CancellationToken";
            }
            else
            {
                yield return parameter.Type.GloballyQualified();
            }
        }
    }
}