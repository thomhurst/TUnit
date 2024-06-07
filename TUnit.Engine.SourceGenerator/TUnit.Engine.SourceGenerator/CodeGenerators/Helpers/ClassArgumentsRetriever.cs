using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class ClassArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> GetClassArguments(INamedTypeSymbol namedTypeSymbol)
    {
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (namedTypeSymbol.InstanceConstructors.SafeFirstOrDefault()?.Parameters.IsDefaultOrEmpty != false)
        {
            return
            [
                new ArgumentsContainer
                {
                    Arguments = [],
                    DataAttributeIndex = null,
                    IsEnumerableData = false,
                    DataAttribute = null
                }
            ];
        }

        return ParseArguments(namedTypeSymbol, className);
    }

    private static IEnumerable<ArgumentsContainer> ParseArguments(INamedTypeSymbol namedTypeSymbol, string className)
    {
        var index = 0;
        
        var classAttributes = namedTypeSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenTestAttribute in classAttributes
                     .Where(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.MethodDataSourceAttribute.WithGlobalPrefix) == true))
        {
            var args = DataSourceDrivenArgumentsRetriever.ParseMethodData(namedTypeSymbol, namedTypeSymbol.Constructors.First(), dataSourceDrivenTestAttribute, VariableNames.ClassArg);

            yield return new ArgumentsContainer
            {
                DataAttribute = dataSourceDrivenTestAttribute,
                DataAttributeIndex = ++index,
                IsEnumerableData = false,
                Arguments = [..args]
            };
        }
        
        foreach (var dataSourceDrivenTestAttribute in classAttributes
                     .Where(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.EnumerableMethodDataAttribute.WithGlobalPrefix) == true))
        {
            var args = DataSourceDrivenArgumentsRetriever.ParseEnumerableMethodData(namedTypeSymbol, namedTypeSymbol.Constructors.First(), dataSourceDrivenTestAttribute, VariableNames.ClassArg);

            yield return new ArgumentsContainer
            {
                DataAttribute = dataSourceDrivenTestAttribute,
                DataAttributeIndex = ++index,
                IsEnumerableData = true,
                Arguments = [..args]
            };
        }

        foreach (var classDataAttribute in classAttributes
                     .Where(x => x.AttributeClass?.IsOrInherits(WellKnownFullyQualifiedClassNames.ClassDataSourceAttribute.WithGlobalPrefix) == true))
        {
            var genericType = classDataAttribute.AttributeClass?.TypeArguments.SafeFirstOrDefault() ?? (ITypeSymbol) classDataAttribute.ConstructorArguments.First().Value!;
            var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            var sharedArgument = classDataAttribute.NamedArguments.SafeFirstOrDefault(x => x.Key == "Shared").Value;

            var sharedArgumentType = sharedArgument.ToCSharpString();
            
            if (sharedArgumentType is "TUnit.Core.SharedType.None" or "" or null)
            {
                yield return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType, $"new {fullyQualifiedGenericType}()")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.Globally")
            {
                yield return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.ForClass")
            {
                yield return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.Keyed")
            {
                var key = sharedArgument.Value?.GetType().GetProperty("Key")?.GetValue(sharedArgument.Value);

                yield return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.ClassDataSourceAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")]
                };
            }
        }
    }
}