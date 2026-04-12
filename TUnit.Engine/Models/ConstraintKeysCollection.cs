using System.Collections;

namespace TUnit.Engine.Models;

internal class ConstraintKeysCollection(IReadOnlyList<string> constraintKeys)
    : IReadOnlyList<string>,
        IEquatable<ConstraintKeysCollection>,
        IComparable<ConstraintKeysCollection>,
        IComparable
{
    private readonly IReadOnlyList<string> _constraintKeys = constraintKeys;

    public IEnumerator<string> GetEnumerator()
    {
        return _constraintKeys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Equals(ConstraintKeysCollection? other)
    {
        if (other == null)
        {
            return false;
        }

        // Constraint key lists are typically 1-2 items; nested loop beats HashSet allocation
        foreach (var key in _constraintKeys)
        {
            foreach (var otherKey in other._constraintKeys)
            {
                if (StringComparer.Ordinal.Equals(key, otherKey))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public int CompareTo(ConstraintKeysCollection? other)
    {
        if (Equals(other, null))
        {
            return 0;
        }

        return -1;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ConstraintKeysCollection) obj);
    }

    public override int GetHashCode()
    {
        var hash = 0;

        foreach (var key in _constraintKeys)
        {
            hash ^= StringComparer.Ordinal.GetHashCode(key);
        }

        return hash;
    }

    public int CompareTo(object? obj)
    {
        return CompareTo(obj as ConstraintKeysCollection);
    }

    public static bool operator ==(ConstraintKeysCollection? left, ConstraintKeysCollection? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ConstraintKeysCollection? left, ConstraintKeysCollection? right)
    {
        return !Equals(left, right);
    }

    public int Count => _constraintKeys.Count;

    public string this[int index] => _constraintKeys[index];

    private sealed class ConstraintKeysCollectionEqualityComparer : IEqualityComparer<ConstraintKeysCollection>
    {
        public bool Equals(ConstraintKeysCollection? x, ConstraintKeysCollection? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(ConstraintKeysCollection obj) => obj.GetHashCode();
    }

    public static IEqualityComparer<ConstraintKeysCollection> ConstraintKeysCollectionComparer { get; } = new ConstraintKeysCollectionEqualityComparer();
}
