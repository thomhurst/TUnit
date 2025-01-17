using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace Inject.NET.SourceGenerator;

internal static class GenericTypeHelper
{
    public static IEnumerable<INamedTypeSymbol> GetConstructedTypes(
        Compilation compilation,
        INamedTypeSymbol genericTypeDefinition)
    {
        var originalGenericDefinition = genericTypeDefinition.OriginalDefinition;

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            
            var serviceProvider = syntaxTree.GetRoot();

            var typeNodes = serviceProvider.DescendantNodes().OfType<TypeSyntax>();

            foreach (var typeNode in typeNodes)
            {
                if (semanticModel.GetTypeInfo(typeNode).Type 
                        is INamedTypeSymbol { IsGenericType: true } typeSymbol 
                    && !typeSymbol.IsGenericDefinition()
                    && SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, originalGenericDefinition))
                {
                    yield return typeSymbol;
                }
            }
        }
    }
}