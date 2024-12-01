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
            TestClassTypeName: namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ArgumentsType: argumentsType,
            TypesToInject: types,
            IsExpandableEnumerable: isExpandableEnumerable,
            IsExpandableFunc: isExpandableFunc,
            IsExpandableTuples: isExpandableTuples,
            IsStatic: isStatic,
            MethodName: methodName,
            TypeName: typeName,
            MethodReturnType: dataSourceMethod.ReturnType,
            ArgumentsExpression: argumentsExpression
        )
        {
            Attribute = methodDataAttribute,
            AttributeIndex = index,
            DisposeAfterTest = disposeAfterTest,
        };
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
        
        if (type is not INamedTypeSymbol namedType)
        {
            return ImmutableArray.Create(dataSourceMethod.ReturnType);
        }

        if (parameterOrPropertyTypes.Length == 1
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(namedType, parameterOrPropertyTypes[0]))
        {
            return ImmutableArray.Create<ITypeSymbol>(namedType);
        }
        
        var genericFunc = context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName());

        if (namedType.IsGenericType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, genericFunc)
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(namedType, genericFunc.Construct(GetTypeOrTuplesType(context.SemanticModel.Compilation, parameterOrPropertyTypes))))
        {
            isExpandableFunc = true;
            namedType = (INamedTypeSymbol)namedType.TypeArguments[0];
        }
        
        if (parameterOrPropertyTypes.Length == 1
            && context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(namedType, parameterOrPropertyTypes[0]))
        {
            return ImmutableArray.Create<ITypeSymbol>(namedType);
        }

        if (namedType.IsTupleType)
        {
            isExpandableTuples = true;
            return namedType.TupleUnderlyingType?.TypeArguments ?? namedType.TypeArguments;
        }
        
        return ImmutableArray.Create<ITypeSymbol>(namedType);
    }

    private static ITypeSymbol GetTypeOrTuplesType(Compilation compilation, ImmutableArray<ITypeSymbol> parameterOrPropertyTypes)
    {
        if (parameterOrPropertyTypes.Length == 1)
        {
            return parameterOrPropertyTypes[0];
        }

        return compilation.CreateTupleTypeSymbol(parameterOrPropertyTypes);
    }

    private static bool TryGetTupleTypes(GeneratorAttributeSyntaxContext context, ImmutableArray<ITypeSymbol> parameterOrPropertyTypes, IMethodSymbol dataSourceMethod, out ImmutableArray<ITypeSymbol> tupleTypes)
    {
        if (parameterOrPropertyTypes.Length <= 1)
        {
            tupleTypes = ImmutableArray<ITypeSymbol>.Empty;
            return false;
        }

        var returnType = dataSourceMethod.ReturnType;
        
        if (returnType is INamedTypeSymbol { IsGenericType: true } enumerableSymbol && 
            SymbolEqualityComparer.Default.Equals(dataSourceMethod.ReturnType.OriginalDefinition,
                context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)))
        {
            returnType = enumerableSymbol.TypeArguments[0];
        }

        if (returnType is INamedTypeSymbol { IsGenericType: true } funcSymbol &&
            SymbolEqualityComparer.Default.Equals(dataSourceMethod.ReturnType.OriginalDefinition,
                context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName())))
        {
            returnType = funcSymbol.TypeArguments[0];
        }

        if (returnType.IsTupleType)
        {
            var tupleType = (INamedTypeSymbol) returnType;
            tupleTypes = tupleType.TupleUnderlyingType?.TypeArguments ?? tupleType.TypeArguments;
            return true;
        }

        tupleTypes = ImmutableArray<ITypeSymbol>.Empty;
        return false;
    }

    private static bool ShouldUnfoldFunc(GeneratorAttributeSyntaxContext context, ImmutableArray<ITypeSymbol> parameterOrPropertyTypes, IMethodSymbol dataSourceMethod)
    {
        if (parameterOrPropertyTypes.Length != 1)
        {
            return false;
        }
        
        var parameterType = parameterOrPropertyTypes[0];

        var returnType = dataSourceMethod.ReturnType;
        
        if (returnType.OriginalDefinition.IsGenericDefinition() && SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition,
                context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)))
        {
            returnType = ((INamedTypeSymbol)returnType).TypeArguments[0];
        }
        
        return SymbolEqualityComparer.Default
                   .Equals(context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Func<object>).GetMetadataName())?.Construct(parameterType),
                       returnType);
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

    private static IEnumerable<ITypeSymbol> CheckTupleTypes(ImmutableArray<ITypeSymbol> parameterOrPropertyTypes, ImmutableArray<ITypeSymbol> tupleTypes)
    {
        for (var index = 0; index < tupleTypes.Length; index++)
        {
            var tupleType = tupleTypes.ElementAtOrDefault(index);
            var parameterType = parameterOrPropertyTypes.ElementAtOrDefault(index);

            yield return tupleType ?? parameterType!;
        }
    }
}