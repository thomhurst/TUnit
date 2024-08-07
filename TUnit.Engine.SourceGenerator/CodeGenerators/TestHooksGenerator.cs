using Microsoft.CodeAnalysis;
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
                    if (model.HookLevel == Core.HookType.EachTest)
                    {
                        TestHooksWriter.Execute(productionContext, model, HookType.SetUp);
                    }
                    else if (model.HookLevel == Core.HookType.Class)
                    {
                        ClassHooksWriter.Execute(productionContext, model, HookType.SetUp);
                    }
                    else if (model.HookLevel == Core.HookType.Assembly)
                    {
                        AssemblyHooksWriter.Execute(productionContext, model, HookType.SetUp);
                    }
                    else if (model.HookLevel == Core.HookType.EachTestGlobally)
                    {
                        GlobalTestHooksWriter.Execute(productionContext, model, HookType.SetUp);
                    }
                }
            });
        
        context.RegisterSourceOutput(cleanUpMethods,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    if (model.HookLevel == Core.HookType.EachTest)
                    {
                        TestHooksWriter.Execute(productionContext, model, HookType.CleanUp);
                    }
                    else if (model.HookLevel == Core.HookType.Class)
                    {
                        ClassHooksWriter.Execute(productionContext, model, HookType.CleanUp);
                    }
                    else if (model.HookLevel == Core.HookType.Assembly)
                    {
                        AssemblyHooksWriter.Execute(productionContext, model, HookType.CleanUp);
                    }
                    else if (model.HookLevel == Core.HookType.EachTestGlobally)
                    {
                        GlobalTestHooksWriter.Execute(productionContext, model, HookType.CleanUp);
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
            var hookLevel = (Core.HookType) Enum.ToObject(typeof(Core.HookType), contextAttribute.ConstructorArguments[0].Value!);

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
                HasTimeoutAttribute = methodSymbol.HasTimeoutAttribute()
            };
        }
    }
}