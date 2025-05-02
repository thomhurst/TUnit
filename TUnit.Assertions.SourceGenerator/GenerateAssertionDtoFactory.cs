// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace TUnit.Assertions.SourceGenerator;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class GenerateAssertionDtoFactory {
    public const string GenerateIsAssertionAttribute = "TUnit.Assertions.GenerateIsAssertionAttribute<TBase>";
    public const string GenerateIsNotAssertionAttribute = "TUnit.Assertions.GenerateIsNotAssertionAttribute<TBase>";
    
    public static GenerateAssertionDto? Create(GeneratorSyntaxContext context, CancellationToken ct) {
        var classNode = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;
                
        // Get the symbol for detailed type information
        ISymbol? classSymbol = semanticModel.GetDeclaredSymbol(classNode);
        if (classSymbol == null) return null;

        // Look for our attributes
        ImmutableArray<AttributeData> attributes = classSymbol.GetAttributes();
                    
        bool hasIsAssertion = attributes
            .Any(attr => attr.AttributeClass?.ToDisplayString() == GenerateIsAssertionAttribute);
                    
        bool hasIsNotAssertion = attributes
            .Any(attr => attr.AttributeClass?.ToDisplayString() == GenerateIsNotAssertionAttribute);

                    
        if (!hasIsAssertion || !hasIsNotAssertion) return null;
                    
        return new GenerateAssertionDto(
            classSymbol
        );
    }
}
