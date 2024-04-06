using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class ClassInvocationsGenerator
{
    public static IEnumerable<ClassInvocationString> GenerateClassInvocations(INamedTypeSymbol namedTypeSymbol)
    {
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        var args = GetClassArguments(namedTypeSymbol);
        
        foreach (var (arguments, argumentsCount) in args)
        {
            if (argumentsCount == ArgumentsCount.Zero)
            {
                yield return new ClassInvocationString($"""
                                                                    object[] classArgs = [];
                                                                    var classInstance = new {className}()
                                                        """, string.Empty);
            }
            if (argumentsCount == ArgumentsCount.One)
            {
                yield return new ClassInvocationString($"""
                                                                    var arg = {arguments};
                                                                    object[] classArgs = [arg];
                                                                    var classInstance = new {className}(arg)
                                                        """, arguments);
            }
            if (argumentsCount == ArgumentsCount.Multiple)
            {
                var stringBuilder = new StringBuilder();
                var splitArguments = arguments.Split(',');
                var variableNames = Enumerable.Range(0, splitArguments.Length).Select(i => $"arg{i}").ToList();
                
                for (var index = 0; index < splitArguments.Length; index++)
                {
                    var argument = splitArguments[index];
                    var variableName = variableNames[index];
                    stringBuilder.AppendLine($"            var {variableName} = {argument};");
                }

                stringBuilder.AppendLine($"            object[] classArgs = [{string.Join(",", variableNames)}]");
                stringBuilder.AppendLine($"            var classInstance = new {className}({string.Join(",", variableNames)});");

                yield return new ClassInvocationString(stringBuilder.ToString(), arguments);
            }
        }
    }
    
    private static IEnumerable<ArgumentString> GetClassArguments(INamedTypeSymbol namedTypeSymbol)
    {
        var className =
            namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        if (namedTypeSymbol.InstanceConstructors.First().Parameters.IsDefaultOrEmpty)
        {
            yield return new ArgumentString(string.Empty, ArgumentsCount.Zero);
        }

        foreach (var dataSourceDrivenTestAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.MethodDataAttribute))
        {
            var arg = dataSourceDrivenTestAttribute.ConstructorArguments.Length == 1
                ? $"{className}.{dataSourceDrivenTestAttribute.ConstructorArguments.First().Value}()"
                : $"{dataSourceDrivenTestAttribute.ConstructorArguments[0].Value}.{dataSourceDrivenTestAttribute.ConstructorArguments[1].Value}()";

            yield return new ArgumentString(arg, ArgumentsCount.One);
        }
        
        foreach (var classDataAttribute in namedTypeSymbol.GetAttributes().Where(x =>
                     x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                         is WellKnownFullyQualifiedClassNames.ClassDataAttribute))
        {
            yield return new ArgumentString($"new {classDataAttribute.ConstructorArguments.First().Value}()", ArgumentsCount.One);
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
                yield return new ArgumentString(
                    $"new {genericType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}()",
                    ArgumentsCount.One);
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.Globally")
            {
                yield return new ArgumentString(
                    $"global::TUnit.Engine.TestDataContainer.InjectedSharedGlobally.GetOrAdd(typeof({fullyQualifiedGenericType}), x => new {fullyQualifiedGenericType}())",
                    ArgumentsCount.One);
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForClass")
            {
                yield return new ArgumentString(
                    $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerClassType.GetOrAdd(new global::TUnit.Engine.Models.DictionaryTypeTypeKey(typeof({className}), typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())",
                    ArgumentsCount.One
                );
            }
            
            if (sharedArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                is "global::TUnit.Core.ForKey")
            {
                var key = sharedArgument.Value?.GetType().GetProperty("Key")?.GetValue(sharedArgument.Value);
                yield return new ArgumentString(
                    $"global::TUnit.Engine.TestDataContainer.InjectedSharedPerKey.GetOrAdd(new global::TUnit.Engine.Models.DictionaryStringTypeKey(\"{key}\", typeof({fullyQualifiedGenericType})), x => new {fullyQualifiedGenericType}())",
                    ArgumentsCount.One);
            }
        }
    }

}