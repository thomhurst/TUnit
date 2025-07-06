namespace TUnit.Core.SourceGenerator.Helpers;

/// <summary>
/// Equality comparer for Type arrays, used for dictionary keys in generic type resolution
/// </summary>
internal sealed class TypeArrayComparer : IEqualityComparer<Type[]>
{
    public static readonly TypeArrayComparer Instance = new();

    public bool Equals(Type[]? x, Type[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
            if (x[i] != y[i])
            {
                return false;
            }

        return true;
    }

    public int GetHashCode(Type[] obj)
    {
        // HashCode is not available in netstandard2.0, use manual hash calculation
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
