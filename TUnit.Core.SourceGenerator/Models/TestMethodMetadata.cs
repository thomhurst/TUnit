using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Contains all the metadata about a test method discovered by the source generator.
/// </summary>
public class TestMethodMetadata
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
}
