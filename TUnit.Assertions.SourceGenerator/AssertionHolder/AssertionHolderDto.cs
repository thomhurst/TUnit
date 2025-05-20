using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TUnit.Assertions.SourceGenerator.GenerateAssertion;

namespace TUnit.Assertions.SourceGenerator.AssertionHolder;

public record AssertionHolderDto(
    INamedTypeSymbol Symbol,
    ImmutableArray<GenerateAssertionDto> GenerateAssertions
) {
    public string ClassName => Symbol.Name;
    public string Namespace => Symbol.ContainingNamespace.ToDisplayString();
    public bool IsEmpty => GenerateAssertions.Length == 0;
}
