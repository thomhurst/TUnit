namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// An equality comparer that uses reference equality (ReferenceEquals) for comparison.
/// Useful when you want to assert that collections contain the exact same object instances,
/// not just structurally equivalent objects.
/// </summary>
/// <typeparam name="T">The type of objects to compare</typeparam>
public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
{
    /// <summary>
    /// Singleton instance of the reference equality comparer.
    /// </summary>
    public static readonly ReferenceEqualityComparer<T> Instance = new();

    private ReferenceEqualityComparer()
    {
    }

    public bool Equals(T? x, T? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(T obj)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
