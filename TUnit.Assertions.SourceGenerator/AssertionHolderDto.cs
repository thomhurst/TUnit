// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

namespace TUnit.Assertions.SourceGenerator;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record AssertionHolderDto(
    INamedTypeSymbol Symbol,
    ImmutableArray<GenerateAssertionDto> GenerateAssertions
) {
    public string ClassName => Symbol.Name;
    public string Namespace => Symbol.ContainingNamespace.ToDisplayString();
    public bool IsEmpty => GenerateAssertions.Length == 0;
    
    public IEnumerable<GenerateAssertionDto> IsAssertions { get; } = GenerateAssertions.Where(static dto => dto.Type == AssertionType.Is);
    public IEnumerable<GenerateAssertionDto> IsNotAssertions { get; }  = GenerateAssertions.Where(static dto => dto.Type == AssertionType.IsNot);
}
