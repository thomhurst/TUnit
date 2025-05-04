// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

namespace TUnit.Assertions.SourceGenerator;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class AssertionHolderDtoFactory {
    public const string GenerateIsNotAssertionAttribute = "TUnit.Assertions.GenerateIsNotAssertionAttribute";
    
    public static AssertionHolderDto? Create(GeneratorSyntaxContext context, CancellationToken ct) {
        var classNode = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;
                
        // Get the symbol for detailed type information
        ISymbol? classSymbol = semanticModel.GetDeclaredSymbol(classNode);
        if (classSymbol is not INamedTypeSymbol symbol) return null;

        // Look for our attributes
        var generateAssertions = GenerateAssertionExtractor.Extract(context, symbol, ct);
        
        return new AssertionHolderDto(
            symbol,
            generateAssertions
        );
    }
}
