using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Context for property injection generation containing all necessary information
/// </summary>
public class PropertyInjectionContext : IEquatable<PropertyInjectionContext>
{
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required string ClassName { get; init; }
    public required string SafeClassName { get; init; }
    public DiagnosticContext? DiagnosticContext { get; init; }

    public bool Equals(PropertyInjectionContext? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return SymbolEqualityComparer.Default.Equals(ClassSymbol, other.ClassSymbol) &&
               ClassName == other.ClassName &&
               SafeClassName == other.SafeClassName;
        // Note: DiagnosticContext is not included in equality as it's contextual/runtime state
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PropertyInjectionContext);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(ClassSymbol);
            hashCode = (hashCode * 397) ^ ClassName.GetHashCode();
            hashCode = (hashCode * 397) ^ SafeClassName.GetHashCode();
            return hashCode;
        }
    }
}