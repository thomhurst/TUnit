using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Contains metadata about a test class and all its test methods.
/// This is used to group tests by class for more efficient file generation.
/// </summary>
public class TestClassMetadata : IEquatable<TestClassMetadata>
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required ImmutableArray<TestMethodMetadata> TestMethods { get; init; }

    public bool Equals(TestClassMetadata? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        // Check if the type symbol is the same
        if (!SymbolEqualityComparer.Default.Equals(TypeSymbol, other.TypeSymbol))
            return false;

        // Check if test methods array length is the same
        if (TestMethods.Length != other.TestMethods.Length)
            return false;

        // Check each test method
        for (int i = 0; i < TestMethods.Length; i++)
        {
            if (!TestMethods[i].Equals(other.TestMethods[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestClassMetadata);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(TypeSymbol);

            // Include test methods in hash
            foreach (var testMethod in TestMethods)
            {
                hashCode = (hashCode * 397) ^ testMethod.GetHashCode();
            }

            return hashCode;
        }
    }
}
