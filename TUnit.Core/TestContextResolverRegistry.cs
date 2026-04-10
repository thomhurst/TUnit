namespace TUnit.Core;

/// <summary>
/// Registry for custom <see cref="ITestContextResolver"/> instances.
/// Registered resolvers are consulted (in registration order) as a fallback when the built-in
/// <c>AsyncLocal</c> chain cannot determine the current <see cref="Context"/>.
/// </summary>
/// <remarks>
/// <para>
/// When no resolvers are registered, the overhead on every <c>Console.Write</c> call
/// is a single volatile array-length check.
/// </para>
/// <para>
/// Register resolvers in a <c>[Before(Assembly)]</c> hook so they are active before any tests run.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Before(Assembly)]
/// public static void SetupResolvers()
/// {
///     TestContextResolverRegistry.Register(new MyCustomResolver());
/// }
/// </code>
/// </example>
public static class TestContextResolverRegistry
{
    // Volatile array for lock-free reads on the hot path.
    // Writes are guarded by _lock to ensure consistency.
    private static volatile ITestContextResolver[] _resolvers = [];
    private static readonly Lock _lock = new();

    /// <summary>
    /// Registers a custom resolver. Resolvers are consulted in registration order.
    /// </summary>
    /// <param name="resolver">The resolver to register.</param>
    public static void Register(ITestContextResolver resolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        lock (_lock)
        {
            var current = _resolvers;

            if (Array.IndexOf(current, resolver) >= 0)
            {
                return;
            }

            var newArray = new ITestContextResolver[current.Length + 1];
            current.CopyTo(newArray, 0);
            newArray[current.Length] = resolver;
            _resolvers = newArray;
        }
    }

    /// <summary>
    /// Removes a previously registered resolver.
    /// </summary>
    /// <param name="resolver">The resolver to remove.</param>
    /// <returns><c>true</c> if the resolver was found and removed; otherwise <c>false</c>.</returns>
    public static bool Unregister(ITestContextResolver resolver)
    {
        if (resolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        lock (_lock)
        {
            var current = _resolvers;
            var index = Array.IndexOf(current, resolver);
            if (index < 0)
            {
                return false;
            }

            var newArray = new ITestContextResolver[current.Length - 1];
            Array.Copy(current, 0, newArray, 0, index);
            Array.Copy(current, index + 1, newArray, index, current.Length - index - 1);
            _resolvers = newArray;
            return true;
        }
    }

    /// <summary>
    /// Consults all registered resolvers in order, returning the first non-null result.
    /// Returns <c>null</c> when no resolver can determine the context (or none are registered).
    /// </summary>
    internal static TestContext? Resolve()
    {
        // Hot-path: volatile read of the array reference.
        // When no resolvers are registered this is just an array-length check.
        var resolvers = _resolvers;
        if (resolvers.Length == 0)
        {
            return null;
        }

        foreach (var resolver in resolvers)
        {
            try
            {
                var context = resolver.ResolveCurrentTestContext();
                if (context is not null)
                {
                    return context;
                }
            }
            catch
            {
                // Swallow exceptions from user-provided resolvers on the hot path.
                // A faulty resolver must not crash Console.Write/WriteLine.
            }
        }

        return null;
    }

    /// <summary>
    /// Removes all registered resolvers. For internal/testing use.
    /// </summary>
    internal static void Clear()
    {
        lock (_lock)
        {
            _resolvers = [];
        }
    }
}
