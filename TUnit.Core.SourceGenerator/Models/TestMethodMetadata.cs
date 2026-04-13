using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Models;

public record CompilationContext(CSharpCompilation Compilation, AttributeWriter AttributeWriter, WellKnownTypes WellKnownTypes);

/// <summary>
/// Contains all the metadata about a test method discovered by the source generator.
/// Uses string-based identity for Equals/GetHashCode so that Roslyn's incremental
/// pipeline can properly cache across compilations (ISymbol instances change on every
/// keystroke, but string representations remain stable for unchanged methods).
/// </summary>
public class TestMethodMetadata : IEquatable<TestMethodMetadata>
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required AttributeData TestAttribute { get; init; }
    public GeneratorAttributeSyntaxContext? Context { get; init; }
    public required CompilationContext CompilationContext { get; init; }
    public required MethodDeclarationSyntax? MethodSyntax { get; init; }
    public bool IsGenericType { get; init; }
    public bool IsGenericMethod { get; init; }

    // Stable string identity fields — populated eagerly from ISymbol during construction.
    // These survive across compilations (same string value for unchanged methods),
    // enabling Roslyn's incremental caching to skip downstream stages.
    public required string MethodFullyQualifiedName { get; init; }
    public required string TypeFullyQualifiedName { get; init; }

    /// <summary>
    /// Hash of all method attributes, computed eagerly from AttributeData.
    /// Detects attribute changes (e.g., adding [Category], changing [Arguments])
    /// without storing ISymbol references in the equality path.
    /// </summary>
    public required int MethodAttributeHash { get; init; }

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

        return MethodFullyQualifiedName == other.MethodFullyQualifiedName &&
               TypeFullyQualifiedName == other.TypeFullyQualifiedName &&
               FilePath == other.FilePath &&
               LineNumber == other.LineNumber &&
               IsGenericType == other.IsGenericType &&
               IsGenericMethod == other.IsGenericMethod &&
               InheritanceDepth == other.InheritanceDepth &&
               MethodAttributeHash == other.MethodAttributeHash;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestMethodMetadata);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = MethodFullyQualifiedName.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeFullyQualifiedName.GetHashCode();
            hashCode = (hashCode * 397) ^ FilePath.GetHashCode();
            hashCode = (hashCode * 397) ^ LineNumber;
            hashCode = (hashCode * 397) ^ IsGenericType.GetHashCode();
            hashCode = (hashCode * 397) ^ IsGenericMethod.GetHashCode();
            hashCode = (hashCode * 397) ^ InheritanceDepth;
            hashCode = (hashCode * 397) ^ MethodAttributeHash;
            return hashCode;
        }
    }

    /// <summary>
    /// Computes a stable hash of method attributes from their string representations.
    /// This avoids storing ISymbol references in the equality path while still
    /// detecting attribute changes across compilations.
    /// </summary>
    public static int ComputeAttributeHash(ImmutableArray<AttributeData> attributes)
    {
        unchecked
        {
            var hash = 17;
            foreach (var attr in attributes)
            {
                var attrName = attr.AttributeClass?.ToDisplayString() ?? "";
                hash = (hash * 31) ^ attrName.GetHashCode();

                foreach (var arg in attr.ConstructorArguments)
                {
                    hash = (hash * 31) ^ HashTypedConstant(arg);
                }

                foreach (var namedArg in attr.NamedArguments)
                {
                    hash = (hash * 31) ^ namedArg.Key.GetHashCode();
                    hash = (hash * 31) ^ HashTypedConstant(namedArg.Value);
                }
            }
            return hash;
        }
    }

    private static int HashTypedConstant(TypedConstant constant)
    {
        unchecked
        {
            if (constant.Kind == TypedConstantKind.Array)
            {
                var hash = 19;
                if (!constant.IsNull)
                {
                    foreach (var element in constant.Values)
                    {
                        hash = (hash * 31) ^ HashTypedConstant(element);
                    }
                }
                return hash;
            }

            var typeHash = constant.Type?.ToDisplayString()?.GetHashCode() ?? 0;
            var valueHash = constant.Value?.ToString()?.GetHashCode() ?? 0;
            return (typeHash * 31) ^ valueHash;
        }
    }
}
