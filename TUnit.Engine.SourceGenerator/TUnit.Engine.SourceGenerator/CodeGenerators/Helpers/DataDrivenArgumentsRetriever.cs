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
    public static IEnumerable<IEnumerable<Argument>> Parse(ImmutableArray<AttributeData> methodAttributes, AttributeData[] testAndClassAttributes)
    {
        return methodAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
            .Select(argumentAttribute => ParseArguments(testAndClassAttributes, argumentAttribute));
    }

    private static IEnumerable<Argument> ParseArguments(AttributeData[] testAndClassAttributes, AttributeData argumentAttribute)
    {
        var objectArray = argumentAttribute.ConstructorArguments.First().Values;

        return objectArray.Select(x =>
            new Argument(ArgumentSource.ArgumentAttribute, TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(x),
                TypedConstantParser.GetTypedConstantValue(x))
        ).WithTimeoutArgument(testAndClassAttributes);
    }
}