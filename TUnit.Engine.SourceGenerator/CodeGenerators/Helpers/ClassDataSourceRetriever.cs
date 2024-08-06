using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class ClassDataSourceRetriever
{
    public static ArgumentsContainer ParseClassData(INamedTypeSymbol namedTypeSymbol, AttributeData classDataAttribute, int index)
    {
        var genericType = classDataAttribute.AttributeClass!.TypeArguments.FirstOrDefault() ?? (INamedTypeSymbol)classDataAttribute.ConstructorArguments[0].Value!;
        var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        var sharedArgument = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Shared").Value;

        var sharedArgumentType = sharedArgument.ToCSharpString();

        var key = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Key").Value.Value as string;
        var arguments = ParseClassDataArguments(sharedArgumentType, key ?? string.Empty, namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), fullyQualifiedGenericType).ToArray();
            
        return new ArgumentsContainer
        {
            DataAttribute = classDataAttribute,
            DataAttributeIndex = index,
            IsEnumerableData = false,
            Arguments = arguments
        };
    }

    private static IEnumerable<Argument> ParseClassDataArguments(string sharedArgumentType, string? key, string className, string fullyQualifiedGenericType)
    {
        if (sharedArgumentType is "TUnit.Core.SharedType.Globally")
        {
            return
            [
                new GloballySharedArgument(fullyQualifiedGenericType,
                    $"TestDataContainer.GetGlobalInstance<{fullyQualifiedGenericType}>(() => new {fullyQualifiedGenericType}())")
            ];
        }
            
        if (sharedArgumentType is "TUnit.Core.SharedType.ForClass")
        {
            return
            [
                new TestClassTypeSharedArgument(fullyQualifiedGenericType,
                    $"TestDataContainer.GetInstanceForType<{fullyQualifiedGenericType}>(typeof({className}), () => new {fullyQualifiedGenericType}())")
                {
                    TestClassType = className
                }
            ];
        }
            
        if (sharedArgumentType is "TUnit.Core.SharedType.Keyed")
        {
            return
            [
                new KeyedSharedArgument(fullyQualifiedGenericType,
                    $"TestDataContainer.GetInstanceForKey<{fullyQualifiedGenericType}>(\"{key}\", () => new {fullyQualifiedGenericType}())")
                {
                    Key = key!
                }
            ];
        }

        return
        [
            new Argument(fullyQualifiedGenericType,
                $"new {fullyQualifiedGenericType}()")
        ];
    }
}