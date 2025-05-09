// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class GenerateAssertionExtractor {
    public const string GenerateIsAssertionAttribute = "TUnit.Assertions.GenerateAssertionAttribute<TBase>";
    
    public static ImmutableArray<GenerateAssertionDto> Extract(GeneratorSyntaxContext context,
        INamedTypeSymbol classSymbol, CancellationToken ct) {
        
        var attributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.ConstructedFrom.ToDisplayString() == GenerateIsAssertionAttribute)
            .ToImmutableArray();
        
        if (attributes.Length == 0) return ImmutableArray<GenerateAssertionDto>.Empty;
        
        ImmutableArray<GenerateAssertionDto>.Builder assertions = ImmutableArray.CreateBuilder<GenerateAssertionDto>();
        foreach (AttributeData attribute in attributes) {
            ITypeSymbol? typeArg = attribute?.AttributeClass?.TypeArguments.FirstOrDefault();
            if (typeArg is null) continue;
            
            var type = (AssertionType)(attribute?.ConstructorArguments[0].Value as int? ?? 0);
            if (type is AssertionType.Undefined) continue;
            
            var methodName = attribute?.ConstructorArguments[1].Value as string;
            if (string.IsNullOrEmpty(methodName)) continue;
            
            // Optional params 
            string? messageFactoryMethodName = null;
            string? expectationExpression = null;

            if (attribute?.ConstructorArguments.Length > 2)
                messageFactoryMethodName = attribute.ConstructorArguments[2].Value as string;
                
            if (attribute?.ConstructorArguments.Length > 3)
                expectationExpression = attribute.ConstructorArguments[3].Value as string;
            
            assertions.Add(new GenerateAssertionDto(
                attributeLocation: attribute?.ApplicationSyntaxReference!.GetSyntax(ct).GetLocation(),
                typeArg,
                type,
                methodName!,
                messageFactoryMethodName,
                expectationExpression
            ));
        }
        
        return assertions.ToImmutableArray();
    }
}