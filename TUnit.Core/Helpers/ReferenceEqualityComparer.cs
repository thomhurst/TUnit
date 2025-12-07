using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Compares objects by reference identity, not value equality.
/// Uses RuntimeHelpers.GetHashCode to get identity-based hash codes.
/// </summary>
public sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    /// <summary>
    /// Singleton instance to avoid repeated allocations.
    /// </summary>
    public static readonly ReferenceEqualityComparer Instance = new();

    /// <summary>
    /// Private constructor to enforce singleton pattern.
    /// </summary>
    private ReferenceEqualityComparer()
    {
    }

    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        // Use RuntimeHelpers.GetHashCode for identity-based hash code
        // This returns the same value as Object.GetHashCode() would if not overridden
        return RuntimeHelpers.GetHashCode(obj);
    }
}
