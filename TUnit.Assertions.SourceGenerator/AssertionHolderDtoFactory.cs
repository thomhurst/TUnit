using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

namespace TUnit.Assertions.SourceGenerator;

public static class AssertionHolderDtoFactory {
    public static AssertionHolderDto? Create(GeneratorSyntaxContext context, CancellationToken ct) {
        var classNode = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;
                
        // Get the symbol for detailed type information
        ISymbol? classSymbol = semanticModel.GetDeclaredSymbol(classNode);
        if (classSymbol is not INamedTypeSymbol symbol) return null;
        
        return new AssertionHolderDto(
            symbol,
            GenerateAssertionFactory.Create(context, symbol, ct)
        );
    }
}
