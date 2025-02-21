using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.Extensions;
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
        var type = GetMethodClass(methodDataAttribute, namedTypeSymbol);
        
        var dataSourceMethod = type
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .First(x =>
            {
                var methodNameIndex = methodDataAttribute.ConstructorArguments.Length == 2 ? 1 : 0;
                return x.Name == methodDataAttribute.ConstructorArguments[methodNameIndex].Value?.ToString();
            });

        var methodName = dataSourceMethod.Name;
        var isStatic = dataSourceMethod.IsStatic;

        var disposeAfterTest =
            methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ??
            true;

        var types = GetInjectableTypes(context, 
            parameterOrPropertyTypes, 
            dataSourceMethod, 
            out var isExpandableEnumerable, 
            out var isExpandableFunc, 
            out var isExpandableTuples);
        
        var argumentsExpression = GetArgumentsExpression(context, methodDataAttribute);
        
        return new MethodDataSourceAttributeContainer
        (
            context.SemanticModel.Compilation,
            TestClassTypeName: namedTypeSymbol.GloballyQualified(),
            ArgumentsType: argumentsType,
            TypesToInject: types,
            IsExpandableEnumerable: isExpandableEnumerable,
            IsExpandableFunc: isExpandableFunc,
            IsExpandableTuples: isExpandableTuples,
            IsStatic: isStatic,
            MethodName: methodName,
            TypeName: type.GloballyQualified(),
            MethodReturnType: dataSourceMethod.ReturnType,
            ArgumentsExpression: argumentsExpression
        )
        {
            Attribute = methodDataAttribute,
            AttributeIndex = index,
            DisposeAfterTest = disposeAfterTest,
        };
    }

    private static ITypeSymbol GetMethodClass(AttributeData methodDataAttribute, INamedTypeSymbol typeContainingAttribute)
    {
        if (methodDataAttribute.AttributeClass?.IsGenericType is true)
        {
            return methodDataAttribute.AttributeClass.TypeArguments[0];
        }

        if (methodDataAttribute.ConstructorArguments.Length is 2)
        {
            return (ITypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
        }

        return typeContainingAttribute;
    }

    private static ImmutableArray<ITypeSymbol> GetInjectableTypes(GeneratorAttributeSyntaxContext context, 
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypes, 
        IMethodSymbol dataSourceMethod,
        out bool isExpandableEnumerable, 
        out bool isExpandableFunc,
        out bool isExpandableTuples)
    {
        isExpandableEnumerable = false;
        isExpandableFunc = false;
        isExpandableTuples = false;
        
        if (parameterOrPropertyTypes.Length == 1
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(dataSourceMethod.ReturnType,
                parameterOrPropertyTypes[0]))
        {
            return ImmutableArray.Create(dataSourceMethod.ReturnType);
        }

        var type = dataSourceMethod.ReturnType;
        if (type.IsIEnumerable(context.SemanticModel.Compilation, out var enumerableInnerType))
        {
            isExpandableEnumerable = true;
            type = enumerableInnerType;
        }
        
        if (type is not INamedTypeSymbol)
        {
            return ImmutableArray.Create(dataSourceMethod.ReturnType);
        }

        if (parameterOrPropertyTypes.Length == 1
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(type, parameterOrPropertyTypes[0]))
        {
            return ImmutableArray.Create(type);
        }
        
        var genericFunc = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName());

        if (type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol 
            && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, genericFunc)
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(type, genericFunc.Construct(GetTypeOrTuplesType(context.SemanticModel.Compilation, parameterOrPropertyTypes))))
        {
            isExpandableFunc = true;
            type = namedTypeSymbol.TypeArguments[0];
        }
        
        if (parameterOrPropertyTypes.Length == 1
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(type, parameterOrPropertyTypes[0]))
        {
            return ImmutableArray.Create(type);
        }

        if (type.IsTupleType && type is INamedTypeSymbol namedTupleType)
        {
            isExpandableTuples = true;
            return namedTupleType.TupleElements.Select(x => x.Type).ToImmutableArray();
        }
        
        return ImmutableArray.Create(type);
    }

    private static ITypeSymbol GetTypeOrTuplesType(Compilation compilation, ImmutableArray<ITypeSymbol> parameterOrPropertyTypes)
    {
        if (parameterOrPropertyTypes.Length == 1)
        {
            return parameterOrPropertyTypes[0];
        }

        return compilation.CreateTupleTypeSymbol(parameterOrPropertyTypes);
    }

    private static string GetArgumentsExpression(GeneratorAttributeSyntaxContext context, AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
        {
            return string.Empty;
        }
        
        var arguments = attributeSyntax.ArgumentList?.Arguments ?? [];

        var argumentsSyntax = arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ToString() == "Arguments")
            ?.Expression;

        if (argumentsSyntax is null)
        {
            return string.Empty;
        }

        return argumentsSyntax.Accept(new CollectionToArgumentsListRewriter(context.SemanticModel))?.ToFullString() ?? string.Empty;
    }
}