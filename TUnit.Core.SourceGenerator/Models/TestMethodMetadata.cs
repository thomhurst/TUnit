using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Contains all the metadata about a test method discovered by the source generator.
/// </summary>
public class TestMethodMetadata : IEquatable<TestMethodMetadata>
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required AttributeData TestAttribute { get; init; }
    public GeneratorAttributeSyntaxContext? Context { get; init; }
    public required MethodDeclarationSyntax? MethodSyntax { get; init; }
    public bool IsGenericType { get; init; }
    public bool IsGenericMethod { get; init; }

    /// <summary>
    /// All attributes on the method, stored for later use during data combination generation
    /// </summary>
    public ImmutableArray<AttributeData> MethodAttributes { get; init; } = ImmutableArray<AttributeData>.Empty;
    
    /// <summary>
    /// The inheritance depth of this test method.
    /// 0 = method is declared directly in the test class
    /// 1 = method is inherited from immediate base class
    /// 2 = method is inherited from base's base class, etc.
    /// </summary>
    public int InheritanceDepth { get; init; } = 0;

    public bool Equals(TestMethodMetadata? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return SymbolEqualityComparer.Default.Equals(MethodSymbol, other.MethodSymbol) &&
               SymbolEqualityComparer.Default.Equals(TypeSymbol, other.TypeSymbol) &&
               FilePath == other.FilePath &&
               LineNumber == other.LineNumber &&
               IsGenericType == other.IsGenericType &&
               IsGenericMethod == other.IsGenericMethod &&
               InheritanceDepth == other.InheritanceDepth;
               // Note: Skipping MethodAttributes comparison to avoid complexity - these rarely change independently
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestMethodMetadata);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(MethodSymbol);
            hashCode = (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(TypeSymbol);
            hashCode = (hashCode * 397) ^ FilePath.GetHashCode();
            hashCode = (hashCode * 397) ^ LineNumber;
            hashCode = (hashCode * 397) ^ IsGenericType.GetHashCode();
            hashCode = (hashCode * 397) ^ IsGenericMethod.GetHashCode();
            hashCode = (hashCode * 397) ^ InheritanceDepth;
            return hashCode;
        }
    }

}
