using System.Collections.Concurrent;
using System.Text;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides a pool of StringBuilder instances to reduce allocations in hot paths.
/// </summary>
internal static class StringBuilderPool
{
    private const int MaxCapacity = 1024;
    private static readonly ConcurrentBag<StringBuilder> Pool = [];

    /// <summary>
    /// Gets a StringBuilder from the pool or creates a new one.
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
    /// </summary>
    public static void Return(StringBuilder builder)
    {
        if (builder.Capacity > MaxCapacity)
        {
            return;
        }

        builder.Clear();
        Pool.Add(builder);
    }
}
