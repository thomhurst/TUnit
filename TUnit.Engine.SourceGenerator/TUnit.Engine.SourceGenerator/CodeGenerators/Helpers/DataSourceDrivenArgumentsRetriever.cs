using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceDrivenArgumentsRetriever
{
    public static IEnumerable<IEnumerable<Argument>> Parse(ImmutableArray<AttributeData> methodAttributes, AttributeData[] testAndClassAttributes)
    {
        var methodData = methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.MethodDataAttribute.WithGlobalPrefix)
            .Select(ParseMethodData)
            .Select(x => x.WithTimeoutArgument(testAndClassAttributes));
        
        var classData = methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                           == WellKnownFullyQualifiedClassNames.ClassDataAttribute.WithGlobalPrefix)
            .Select(ParseClassData)
            .Select(x => x.WithTimeoutArgument(testAndClassAttributes));

        return methodData.Concat(classData);
    }

    private static IEnumerable<Argument> ParseMethodData(AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            return [new Argument("var", $"{methodDataAttribute.ConstructorArguments.First().Value!}()")];
        }

        var type = methodDataAttribute.ConstructorArguments[0].Type!;
        return [new Argument("var", $"{type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{methodDataAttribute.ConstructorArguments[1].Value!}()")];
    }
    
    private static IEnumerable<Argument> ParseClassData(AttributeData classDataAttribute)
    {
        var type = (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedType = type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return [new Argument(fullyQualifiedType, $"new {fullyQualifiedType}()")];
    }
}