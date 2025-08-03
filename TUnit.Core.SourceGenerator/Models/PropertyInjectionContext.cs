using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Context for property injection generation containing all necessary information
/// </summary>
public class PropertyInjectionContext
{
    public required INamedTypeSymbol ClassSymbol { get; init; }
    public required string ClassName { get; init; }
    public required string SafeClassName { get; init; }
    public DiagnosticContext? DiagnosticContext { get; init; }
}