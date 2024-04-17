using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataDrivenArgumentsRetriever
{
    public static IEnumerable<IEnumerable<Argument>> Parse(AttributeData[] testAndClassAttributes)
    {
        return testAndClassAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithGlobalPrefix)
            .Select(argumentAttribute => ParseArguments(testAndClassAttributes, argumentAttribute));
    }

    private static IEnumerable<Argument> ParseArguments(AttributeData[] testAndClassAttributes, AttributeData argumentAttribute)
    {
        var objectArray = argumentAttribute.ConstructorArguments.First().Values;

        return objectArray.Select(x =>
            new Argument(x.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                TypedConstantParser.GetTypedConstantValue(x))
        ).WithTimeoutArgument(testAndClassAttributes);
    }
}