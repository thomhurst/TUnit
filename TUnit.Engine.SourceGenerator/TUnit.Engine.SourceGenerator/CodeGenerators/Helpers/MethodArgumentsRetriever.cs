using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodArgumentsRetriever
{
    public static IEnumerable<Argument> GetMethodArguments(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            yield break;
        }
        
        var testAttribute = methodSymbol.GetTestAttribute();

        var testType = testAttribute.GetTestType();
        
        switch (testType)
        {
            case TestType.Basic:
                yield break;
            case TestType.DataDriven:
                yield return ParseDataDrivenAttributes(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));
            case TestType.DataSourceDriven:
                yield return ParseDataSourceDrivenAttributes(methodSymbol.GetAttributesIncludingClass(namedTypeSymbol));
            case TestType.Combinative:
                yield return ParseDataDrivenAttributes(methodSymbol.Parameters);
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var allTestAttributes = MethodExtensions.GetAttributesIncludingClass(methodSymbol, namedTypeSymbol);
        
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (namedTypeSymbol.InstanceConstructors.First().Parameters.IsDefaultOrEmpty)
        {
            yield return Argument.NoArguments;
            yield break;
        }

        foreach (var dataSourceDrivenTestAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.MethodDataAttribute))
        {
            var arg = dataSourceDrivenTestAttribute.ConstructorArguments.Length == 1
                ? $"{className}.{dataSourceDrivenTestAttribute.ConstructorArguments.First().Value}()"
                : $"{TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(dataSourceDrivenTestAttribute.ConstructorArguments[0])}.{dataSourceDrivenTestAttribute.ConstructorArguments[1].Value}()";

            yield return new Argument("var", arg);
        }
        
        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.ClassDataAttribute))
        {
            var fullyQualifiedTypeNameFromTypedConstantValue = TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(classDataAttribute.ConstructorArguments.First());
            yield return new Argument(fullyQualifiedTypeNameFromTypedConstantValue, $"new {fullyQualifiedTypeNameFromTypedConstantValue}()");
        }
        
        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is "global::TUnit.Core.InjectAttribute"))
        {
            var genericType = classDataAttribute.AttributeClass!.TypeArguments.First();
            var fullyQualifiedGenericType = genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            var sharedArgument = classDataAttribute.NamedArguments.First(x => x.Key == "Shared").Value;

            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.None")
            {
                yield return new Argument(fullyQualifiedGenericType, $"new {fullyQualifiedGenericType}()");
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.Globally")
            {
                yield return new Argument(fullyQualifiedGenericType, $"global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())");
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForClass")
            {
                yield return new Argument(fullyQualifiedGenericType, $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())");
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForKey")
            {
                var key = sharedArgument.Value?.GetType().GetProperty("Key")?.GetValue(sharedArgument.Value);
                yield return new Argument(fullyQualifiedGenericType, $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())");
            }
        }
    }

    private static Argument ParseDataDrivenAttributes(AttributeData[] getAttributesIncludingClass)
    {
        throw new NotImplementedException();
    }
}