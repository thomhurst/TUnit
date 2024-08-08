using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers.Hooks;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

[Generator]
internal class GlobalTestHooksGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var setUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.GlobalBeforeAttribute",
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        var cleanUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.GlobalAfterAttribute",
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        context.RegisterSourceOutput(setUpMethods,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    GlobalTestHooksWriter.Execute(productionContext, model, HookType.SetUp);
                }
            });
        
        context.RegisterSourceOutput(cleanUpMethods,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    GlobalTestHooksWriter.Execute(productionContext, model, HookType.CleanUp);
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

        if (!methodSymbol.IsStatic || methodSymbol.IsAbstract || methodSymbol.DeclaredAccessibility != Accessibility.Public)
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