using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Compares objects by reference identity, not value equality.
/// Uses RuntimeHelpers.GetHashCode to get identity-based hash codes.
/// </summary>
public class ReferenceEqualityComparer : IEqualityComparer<object>
{
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
