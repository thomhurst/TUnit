// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace TUnit.Assertions.SourceGenerator;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class GenerateAssertionDto(
    ISymbol symbol,
    ImmutableArray<AttributeData> isAssertions, 
    ImmutableArray<AttributeData> isNotAssertions
) {
    public string ClassName => symbol.Name;
    public string Namespace => symbol.ContainingNamespace.ToDisplayString();
    public bool IsEmpty => isAssertions.Length == 0 && isNotAssertions.Length == 0;
    // public bool IsEmpty => false;
    
    public IEnumerable<AttributeData> IsAssertions { get; } = isAssertions;
    public IEnumerable<AttributeData> IsNotAssertions { get; } = isNotAssertions;
}
