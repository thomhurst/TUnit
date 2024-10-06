using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers.Hooks;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

[Generator]
internal class StaticClassDataSourceInjectorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var staticClassDataSourceInjectors = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.ClassDataSourceAttribute`1",
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        
        context.RegisterSourceOutput(staticClassDataSourceInjectors,
            (productionContext, models) =>
            {
                foreach (var model in models)
                {
                    StaticClassDataSourceInjectorWriter.Execute(productionContext, model);
                }
            });
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax;
    }

    static IEnumerable<StaticClassDataSourceInjectorModel> GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IPropertySymbol propertySymbol)
        {
            yield break;
        }

        if (!propertySymbol.IsStatic || propertySymbol.IsAbstract || propertySymbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
        {
            yield break;
        }

        foreach (var contextAttribute in context.Attributes
                     .Where(contextAttribute => contextAttribute.NamedArguments
                         .Any(static a => a.Key == "Shared" && a.Value.ToCSharpString() == "TUnit.Core.SharedType.Globally")))
        {
            var injectableType = contextAttribute.AttributeClass!.TypeArguments.First();
            
            yield return new StaticClassDataSourceInjectorModel
            {
                MinimalTypeName = propertySymbol.ContainingType.Name, FullyQualifiedTypeName = propertySymbol.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), PropertyName = propertySymbol.Name, InjectableType = injectableType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            };
        }
    }
}