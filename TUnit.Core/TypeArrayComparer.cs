using System;
using System.Collections.Generic;

namespace TUnit.Core;

/// <summary>
/// Efficient equality comparer for Type arrays used in generic test registration
/// </summary>
public sealed class TypeArrayComparer : IEqualityComparer<Type[]>
{
    /// <summary>
    /// Singleton instance for reuse
    /// </summary>
    public static readonly TypeArrayComparer Instance = new();
    
    /// <summary>
    /// Private constructor to enforce singleton pattern
    /// </summary>
    private TypeArrayComparer() { }
    
    /// <summary>
    /// Compares two Type arrays for equality
    /// </summary>
    public bool Equals(Type[]? x, Type[]? y)
    {
        // Fast reference equality check
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        // Handle null cases
        if (x is null || y is null)
        {
            return false;
        }

        // Check length first (fastest check)
        if (x.Length != y.Length)
        {
            return false;
        }

        // Compare each type
        for (var i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Computes hash code for Type array
    /// </summary>
    public int GetHashCode(Type[] obj)
    {
        if (obj is null)
        {
            return 0;
        }

        // Use simple but effective hash combination for .NET Standard 2.0 compatibility
        unchecked
        {
            var hash = 17;
            foreach (var type in obj)
            {
                hash = hash * 31 + (type?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}