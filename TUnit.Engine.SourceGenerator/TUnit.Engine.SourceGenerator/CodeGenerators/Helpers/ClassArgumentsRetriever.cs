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
    public static ArgumentsContainer GetClassArguments(INamedTypeSymbol namedTypeSymbol)
    {
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (namedTypeSymbol.InstanceConstructors.First().Parameters.IsDefaultOrEmpty)
        {
            return new ArgumentsContainer
            {
                Arguments = [],
                DataAttributeIndex = null,
                IsEnumerableData = false,
                DataAttribute = null
            };
        }

        var index = 0;
        foreach (var dataSourceDrivenTestAttribute in namedTypeSymbol.GetAttributes()
                     .Where(x => x.GetFullyQualifiedAttributeTypeName() 
                                 == WellKnownFullyQualifiedClassNames.MethodDataAttribute.WithGlobalPrefix))
        {
            var arg = dataSourceDrivenTestAttribute.ConstructorArguments.Length == 1
                ? $"{className}.{dataSourceDrivenTestAttribute.ConstructorArguments.First().Value}()"
                : $"{TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(dataSourceDrivenTestAttribute.ConstructorArguments[0])}.{dataSourceDrivenTestAttribute.ConstructorArguments[1].Value}()";

            return new ArgumentsContainer
            {
                DataAttribute = dataSourceDrivenTestAttribute,
                DataAttributeIndex = ++index,
                IsEnumerableData = false,
                Arguments = [new Argument(ArgumentSource.MethodDataAttribute, "var", arg)]
            };
        }

        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes()
                     .Where(x => x.GetFullyQualifiedAttributeTypeName()
                                 == WellKnownFullyQualifiedClassNames.ClassDataAttribute.WithGlobalPrefix)) 
        {
            var fullyQualifiedTypeNameFromTypedConstantValue = TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(classDataAttribute.ConstructorArguments.First());
           
            return new ArgumentsContainer
            {
                DataAttribute = classDataAttribute,
                DataAttributeIndex = ++index,
                IsEnumerableData = false,
                Arguments = [new Argument(ArgumentSource.ClassDataAttribute, fullyQualifiedTypeNameFromTypedConstantValue, $"new {fullyQualifiedTypeNameFromTypedConstantValue}()")]
            };
        }

        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes()
                     .Where(x => x.GetFullyQualifiedAttributeTypeName()
                         is "global::TUnit.Core.InjectAttribute"))
        {
            var genericType = classDataAttribute.AttributeClass!.TypeArguments.First();
            var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            var sharedArgument = classDataAttribute.NamedArguments.First(x => x.Key == "Shared").Value;

            var sharedArgumentType = sharedArgument.ToCSharpString();
            
            if (sharedArgumentType is "TUnit.Core.SharedType.None")
            {
                return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.InjectAttribute, fullyQualifiedGenericType, $"new {fullyQualifiedGenericType}()")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.Globally")
            {
                return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.InjectAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.ForClass")
            {
                return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.InjectAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")]
                };
            }
            
            if (sharedArgumentType is "TUnit.Core.SharedType.Keyed")
            {
                var key = sharedArgument.Value?.GetType().GetProperty("Key")?.GetValue(sharedArgument.Value);
                
                return new ArgumentsContainer
                {
                    DataAttribute = classDataAttribute,
                    DataAttributeIndex = ++index,
                    IsEnumerableData = false,
                    Arguments = [new Argument(ArgumentSource.InjectAttribute, fullyQualifiedGenericType, $"({fullyQualifiedGenericType})global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())")]
                };
            }
        }
        
        return new ArgumentsContainer
        {
            Arguments = [],
            DataAttributeIndex = null,
            IsEnumerableData = false,
            DataAttribute = null
        };
    }
}