using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;


[Generator]
public class AssemblySetUpCleanUpGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var testMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(testMethods, Execute);
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    static IMethodSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax)
        {
            return null;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (!methodSymbol.IsStatic)
        {
            return null;
        }

        var attributes = methodSymbol.GetAttributes();

        if (!attributes.Any(x =>
                x.AttributeClass?.BaseType?.ToDisplayString(
                    DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
                is WellKnownFullyQualifiedClassNames.AssemblySetUpAttribute
                or WellKnownFullyQualifiedClassNames.AssemblyCleanUpAttribute))
        {
            return null;
        }

        return methodSymbol;
    }

    private void Execute(SourceProductionContext context, ImmutableArray<IMethodSymbol?> methodSymbols)
    {
        foreach (var method in methodSymbols)
        {
            
        }
    }
}