using System.Collections;

namespace TUnit.Engine.Models;

internal class ConstraintKeysCollection(IReadOnlyList<string> constraintKeys)
    : IReadOnlyList<string>, IEquatable<ConstraintKeysCollection>
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
        
        return _constraintKeys.Intersect(other._constraintKeys).Any();
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

        return Equals((ConstraintKeysCollection)obj);
    }

    public override int GetHashCode()
    {
        return 1;
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

            if (x == null)
            {
                return false;
            }
            
            if (y == null)
            {
                return false;
            }
            
            return x._constraintKeys.Intersect(y._constraintKeys).Any();
        }

        public int GetHashCode(ConstraintKeysCollection obj)
        {
            return 1;
        }
    }

    public static IEqualityComparer<ConstraintKeysCollection> ConstraintKeysCollectionComparer { get; } = new ConstraintKeysCollectionEqualityComparer();
}