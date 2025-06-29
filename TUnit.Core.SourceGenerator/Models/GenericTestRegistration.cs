using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Model representing a generic test class registration for AOT support.
/// </summary>
public record GenericTestRegistration
{
    /// <summary>
    /// The generic type definition (e.g., MyTest<>).
    /// </summary>
    public required INamedTypeSymbol GenericTypeDefinition { get; init; }

    /// <summary>
    /// The concrete type arguments (e.g., int, string).
    /// </summary>
    public required ImmutableArray<ITypeSymbol> TypeArguments { get; init; }

    /// <summary>
    /// The fully qualified name of the concrete type (e.g., MyTest<int>).
    /// </summary>
    public required string ConcreteTypeName { get; init; }

    /// <summary>
    /// The constructed concrete type.
    /// </summary>
    public required INamedTypeSymbol ConcreteType { get; init; }
}
