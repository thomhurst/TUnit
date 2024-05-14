using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataDrivenArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> Parse(ImmutableArray<AttributeData> methodAttributes, AttributeData[] testAndClassAttributes)
    {
        var index = 0;
        return methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
            .Select(argumentAttribute => ParseArguments(testAndClassAttributes, argumentAttribute, ++index));
    }

    private static ArgumentsContainer ParseArguments(AttributeData[] testAndClassAttributes, AttributeData argumentAttribute, int dataAttributeIndex)
    {
        var objectArray = argumentAttribute.ConstructorArguments.SafeFirstOrDefault().Values;

        return new ArgumentsContainer
        {
            DataAttribute = argumentAttribute,
            DataAttributeIndex = dataAttributeIndex,
            IsEnumerableData = false,
            Arguments = [..objectArray.Select(x =>
                new Argument(ArgumentSource.ArgumentAttribute, TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(x),
                    TypedConstantParser.GetTypedConstantValue(x))
            ).WithTimeoutArgument(testAndClassAttributes)]
        };
    }
}