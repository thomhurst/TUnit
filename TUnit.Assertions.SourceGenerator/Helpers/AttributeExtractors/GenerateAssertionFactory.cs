using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

public static class GenerateAssertionFactory {
    public const string GenerateIsAssertionAttribute = "TUnit.Assertions.GenerateAssertionAttribute<TBase>";
    
    public static ImmutableArray<GenerateAssertionDto> Create(
        GeneratorSyntaxContext context,
        INamedTypeSymbol classSymbol,
        CancellationToken ct
    ) {
        
        var attributes = classSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.ConstructedFrom.ToDisplayString() == GenerateIsAssertionAttribute)
            .ToImmutableArray();
        
        if (attributes.Length == 0) return ImmutableArray<GenerateAssertionDto>.Empty;
        
        ImmutableArray<GenerateAssertionDto>.Builder assertions = ImmutableArray.CreateBuilder<GenerateAssertionDto>();
        foreach (AttributeData attribute in attributes) {
            ITypeSymbol? typeArg = attribute?.AttributeClass?.TypeArguments.FirstOrDefault();
            if (typeArg is null) continue;

            if (attribute?.ConstructorArguments[0].Value is not int type || !Enum.IsDefined(typeof(AssertionType), type)) continue;
            var assertionType = (AssertionType)type;
            
            string? methodName = attribute?.ConstructorArguments[1].Value as string;
            if (string.IsNullOrEmpty(methodName)) continue;
            
            // Optional params 
            string? messageFactoryMethodName = null;
            string? expectationExpression = null;

            if (attribute?.ConstructorArguments.Length > 2)
                messageFactoryMethodName = attribute.ConstructorArguments[2].Value as string;
                
            if (attribute?.ConstructorArguments.Length > 3)
                expectationExpression = attribute.ConstructorArguments[3].Value as string;
            
            assertions.Add(new GenerateAssertionDto(
                AttributeLocation: attribute?.ApplicationSyntaxReference!.GetSyntax(ct).GetLocation(),
                typeArg,
                assertionType,
                methodName!,
                messageFactoryMethodName,
                expectationExpression
            ));
        }
        
        return assertions.ToImmutableArray();
    }
}