using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers.Hooks;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

[Generator]
internal class TestHooksGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var setUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeAttribute",
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        var cleanUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterAttribute",
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        context.RegisterSourceOutput(setUpMethods,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    if (model.HookLevel == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(productionContext, model, HookLocationType.Before);
                    }
                    else if (model.HookLevel == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(productionContext, model, HookLocationType.Before);
                    }
                    else if (model.HookLevel == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(productionContext, model, HookLocationType.Before);
                    }
                    else if (model.HookLevel is "TestDiscovery" or "TestSession")
                    {
                        GlobalTestHooksWriter.Execute(productionContext, model, HookLocationType.Before);
                    }
                }
            });
        
        context.RegisterSourceOutput(cleanUpMethods,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    if (model.HookLevel == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(productionContext, model, HookLocationType.After);
                    }
                    else if (model.HookLevel == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(productionContext, model, HookLocationType.After);
                    }
                    else if (model.HookLevel == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(productionContext, model, HookLocationType.After);
                    }
                    else if (model.HookLevel is "TestDiscovery" or "TestSession")
                    {
                        GlobalTestHooksWriter.Execute(productionContext, model, HookLocationType.After);
                    }
                }
            });
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax;
    }

    static IEnumerable<HooksDataModel> GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            yield break;
        }

        foreach (var contextAttribute in context.Attributes)
        {
            var hookLevel = contextAttribute.ConstructorArguments[0].ToCSharpString();

            yield return new HooksDataModel
            {
                MethodName = methodSymbol.Name,
                HookLevel = hookLevel,
                FullyQualifiedTypeName =
                    methodSymbol.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                MinimalTypeName = methodSymbol.ContainingType.Name,
                ParameterTypes = methodSymbol.Parameters
                    .Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
                    .ToArray(),
                HasTimeoutAttribute = methodSymbol.HasTimeoutAttribute(),
                HookExecutor = methodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::TUnit.Core.Executors.HookExecutorAttribute") == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                Order = contextAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Order").Value.Value as int? ?? 0,
                FilePath = contextAttribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty,
                LineNumber = contextAttribute.ConstructorArguments[2].Value as int? ?? 0,
            };
        }
    }
}