namespace TUnit.Assertions.Conditions.Helpers;

/// <summary>
/// An IEqualityComparer implementation that uses a custom Func for equality comparison.
/// This allows users to pass lambda predicates to assertion methods like Using().
/// </summary>
/// <typeparam name="T">The type of objects to compare.</typeparam>
internal sealed class FuncEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T?, T?, bool> _equals;

    public FuncEqualityComparer(Func<T?, T?, bool> equals)
    {
        _equals = equals ?? throw new ArgumentNullException(nameof(equals));
    }

    public bool Equals(T? x, T? y) => _equals(x, y);

    // Return a constant hash code to force linear search in collection equivalency.
    // This is intentional because:
    // 1. We cannot derive a meaningful hash function from an equality predicate
    // 2. CollectionEquivalencyChecker already uses O(n²) linear search for custom comparers
    // 3. This matches the expected behavior for all custom IEqualityComparer implementations
    public int GetHashCode(T obj) => 0;
}
