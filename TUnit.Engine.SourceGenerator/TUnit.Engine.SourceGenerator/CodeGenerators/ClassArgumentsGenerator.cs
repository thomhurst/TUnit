using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

public static class ClassArgumentsGenerator
{
    public static IEnumerable<string> GetClassArguments(INamedTypeSymbol namedTypeSymbol)
    {
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (namedTypeSymbol.InstanceConstructors.First().Parameters.IsDefaultOrEmpty)
        {
            yield break;
        }

        foreach (var dataSourceDrivenTestAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.MethodDataAttribute))
        {
            var arg = dataSourceDrivenTestAttribute.ConstructorArguments.Length == 1
                ? $"{className}.{dataSourceDrivenTestAttribute.ConstructorArguments.First().Value}()"
                : $"{TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(dataSourceDrivenTestAttribute.ConstructorArguments[0])}.{dataSourceDrivenTestAttribute.ConstructorArguments[1].Value}()";

            yield return arg;
        }
        
        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.ClassDataAttribute))
        {
            yield return $"new {TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(classDataAttribute.ConstructorArguments.First())}()";
        }
        
        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is "global::TUnit.Core.InjectAttribute"))
        {
            var genericType = classDataAttribute.AttributeClass!.TypeArguments.First();
            var fullyQualifiedGenericType =
                genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            var sharedArgument = classDataAttribute.NamedArguments.First(x => x.Key == "Shared").Value;

            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.None")
            {
                yield return $"new {genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}()";
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.Globally")
            {
                yield return $"global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())";
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForClass")
            {
                yield return $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())";
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForKey")
            {
                var key = sharedArgument.Value?.GetType().GetProperty("Key")?.GetValue(sharedArgument.Value);
                yield return $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())";
            }
        }
    }
}