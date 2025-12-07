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

    /// <summary>
    /// Compares two objects by reference identity.
    /// </summary>
    /// <remarks>
    /// The 'new' keyword is used because this method explicitly implements
    /// IEqualityComparer&lt;object&gt;.Equals with nullable parameters, which
    /// hides the inherited static Object.Equals(object?, object?) method.
    /// This is intentional and provides the correct behavior for reference equality.
    /// </remarks>
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
