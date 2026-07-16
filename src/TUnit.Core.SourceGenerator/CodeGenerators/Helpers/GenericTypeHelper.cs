using System.Diagnostics.CodeAnalysis;
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
                if (semanticModel.GetTypeInfo(typeNode).Type is not INamedTypeSymbol { IsGenericType: true } typeSymbol
                    || typeSymbol.IsGenericDefinition())
                {
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, originalGenericDefinition))
                {
                    yield return typeSymbol;
                    continue;
                }

                if (IsAncestor(originalGenericDefinition, typeSymbol, out var matchingTypeParameter))
                {
                    yield return matchingTypeParameter;
                }
            }
        }
    }

    private static bool IsAncestor(INamedTypeSymbol genericTypeDefinition, INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out INamedTypeSymbol? foundMatch)
    {
        if (typeSymbol.GetBaseTypes()
            .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, genericTypeDefinition))
            is not { } matchingType)
        {
            foundMatch = null;
            return false;
        }

        if (matchingType is not { })
        {
            foundMatch = null;
            return false;
        }

        foundMatch = matchingType;
        return true;
    }
}
