using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TUnit.Assertions.SourceGenerator.Helpers.AttributeExtractors;

namespace TUnit.Assertions.SourceGenerator;

public record AssertionHolderDto(
    INamedTypeSymbol Symbol,
    ImmutableArray<GenerateAssertionDto> GenerateAssertions
) {
    public string ClassName => Symbol.Name;
    public string Namespace => Symbol.ContainingNamespace.ToDisplayString();
    public bool IsEmpty => GenerateAssertions.Length == 0;
}
