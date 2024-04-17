using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceDrivenArgumentsRetriever
{
    public static IEnumerable<IEnumerable<Argument>> Parse(AttributeData[] testAndClassAttributes)
    {
        var methodData = testAndClassAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                 == WellKnownFullyQualifiedClassNames.MethodDataAttribute.WithGlobalPrefix)
            .Select(ParseMethodData);
        
        var classData = testAndClassAttributes.Where(x => x.GetFullyQualifiedAttributeTypeName()
                                                           == WellKnownFullyQualifiedClassNames.MethodDataAttribute.WithGlobalPrefix)
            .Select(ParseClassData);

        return methodData.Concat(classData);
    }

    private static IEnumerable<Argument> ParseMethodData(AttributeData methodDataAttribute)
    {
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            yield return new Argument("var", $"{methodDataAttribute.ConstructorArguments.First().Value!}()");
        }

        var type = (INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
        yield return new Argument("var", $"{type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{methodDataAttribute.ConstructorArguments[1].Value!}()");
    }
    
    private static IEnumerable<Argument> ParseClassData(AttributeData classDataAttribute)
    {
        var type = (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedType = type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        yield return new Argument(fullyQualifiedType, $"new {fullyQualifiedType}()");
    }
}