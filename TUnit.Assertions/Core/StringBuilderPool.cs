using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Assertions.Core;

/// <summary>
/// Provides a lightweight pool of StringBuilder instances for assertion expression building.
/// Reduces allocations during test execution by reusing StringBuilder objects.
/// Thread-safe implementation using ConcurrentBag.
/// </summary>
internal static class StringBuilderPool
{
    private static readonly ConcurrentBag<StringBuilder> Pool = new();
    private const int MaxCapacity = 1024; // Discard builders exceeding 1KB to prevent memory bloat

    /// <summary>
    /// Gets a StringBuilder from the pool, or creates a new one if the pool is empty.
    /// </summary>
    public static StringBuilder Get()
    {
        if (Pool.TryTake(out var builder))
        {
            return builder;
        }

        return new StringBuilder();
    }

    /// <summary>
    /// Returns a StringBuilder to the pool after clearing its contents.
    /// Discards builders with excessive capacity to prevent memory bloat.
    /// </summary>
    public static void Return(StringBuilder? builder)
    {
        if (builder == null)
        {
            return;
        }

        builder.Clear();

        // Discard if capacity is excessive to prevent memory bloat
        if (builder.Capacity > MaxCapacity)
        {
            return;
        }

        Pool.Add(builder);
    }
}
