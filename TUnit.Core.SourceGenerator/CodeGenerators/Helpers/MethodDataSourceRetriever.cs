using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class MethodDataSourceRetriever
{
    public static ArgumentsContainer ParseMethodData(GeneratorAttributeSyntaxContext context,
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypes,
        INamedTypeSymbol namedTypeSymbol, AttributeData methodDataAttribute, ArgumentsType argumentsType, int index)
    {
        string typeName;
        IMethodSymbol? dataSourceMethod;
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            typeName =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = namedTypeSymbol
                .GetMembersIncludingBase()
                .OfType<IMethodSymbol>()
                .First(x => x.Name == methodDataAttribute.ConstructorArguments[0].Value!.ToString());
        }
        else
        {
            var typeContainingDataSourceMethod = (INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
            
            typeName = typeContainingDataSourceMethod.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = typeContainingDataSourceMethod
                .GetMembersIncludingBase()
                .OfType<IMethodSymbol>()
                .First(x => x.Name == methodDataAttribute.ConstructorArguments[1].Value!.ToString());
        }

        var methodName = dataSourceMethod.Name;
        var isStatic = dataSourceMethod.IsStatic;

        var disposeAfterTest =
            methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ??
            true;

        var isEnumerable = dataSourceMethod.ReturnType.EnumerableGenericTypeIs(context, parameterOrPropertyTypes, out var innerType);
        
        if (!isEnumerable)
        {
            innerType = dataSourceMethod.ReturnType!;
        }

        var argumentsExpression = GetArgumentsExpression(context, methodDataAttribute);
        
        if (parameterOrPropertyTypes.Length > 1 && innerType!.IsTupleType && innerType is INamedTypeSymbol typeSymbol)
        {
            var tupleTypes = typeSymbol.TupleUnderlyingType?.TypeArguments ??
                             typeSymbol.TypeArguments;

            if (CheckTupleTypes(parameterOrPropertyTypes, tupleTypes) is {} result)
            {
                return new MethodDataSourceAttributeContainer
                (
                    TestClassTypeName: namedTypeSymbol.ToDisplayString(DisplayFormats
                        .FullyQualifiedGenericWithGlobalPrefix),
                    ArgumentsType: argumentsType,
                    IsEnumerableData: isEnumerable,
                    IsStatic: isStatic,
                    MethodName: methodName,
                    TypeName: typeName,
                    MethodReturnType: dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats
                        .FullyQualifiedGenericWithGlobalPrefix),
                    TupleTypes: result.ToArray(),
                    ArgumentsExpression: argumentsExpression
                )
                {
                    Attribute = methodDataAttribute,
                    AttributeIndex = index,
                    DisposeAfterTest = disposeAfterTest,
                };
            }
        }

        return new MethodDataSourceAttributeContainer
        (
            TestClassTypeName: namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ArgumentsType: argumentsType,
            IsEnumerableData: isEnumerable,
            IsStatic: isStatic,
            MethodName: methodName,
            TypeName: typeName,
            MethodReturnType: dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix),
            TupleTypes: [],
            ArgumentsExpression: argumentsExpression
        )
        {
            Attribute = methodDataAttribute,
            AttributeIndex = index,
            DisposeAfterTest = disposeAfterTest,
        };
    }

    private static string GetArgumentsExpression(GeneratorAttributeSyntaxContext context, AttributeData methodDataAttribute)
    {
        var attributeSyntax = (AttributeSyntax)methodDataAttribute.ApplicationSyntaxReference!.GetSyntax();

        var arguments = attributeSyntax.ArgumentList!.Arguments;

        var argumentsSyntax = arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ToString() == "Arguments")
            ?.Expression;

        if (argumentsSyntax is null)
        {
            return string.Empty;
        }

        return argumentsSyntax.Accept(new CollectionToArgumentsListRewriter(context.SemanticModel))?.ToFullString() ?? string.Empty;
    }

    private static IEnumerable<string> CheckTupleTypes(ImmutableArray<ITypeSymbol> parameterOrPropertyTypes, ImmutableArray<ITypeSymbol> tupleTypes)
    {
        for (var index = 0; index < tupleTypes.Length; index++)
        {
            var tupleType = tupleTypes.ElementAtOrDefault(index);
            var parameterType = parameterOrPropertyTypes.ElementAtOrDefault(index);

            yield return tupleType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
                ?? parameterType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)!;
        }
    }
}