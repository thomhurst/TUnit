using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Core.Tracking;

namespace TUnit.Core;

/// <summary>
/// Provides shared instance management for custom data sources.
/// Use this class to implement custom data source attributes that need the same
/// sharing behavior as <see cref="ClassDataSourceAttribute{T}"/>.
/// </summary>
public static class SharedDataSources
{
    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type, using the default constructor.
    /// </summary>
    /// <typeparam name="T">The type of instance to get or create. Must have a parameterless constructor.</typeparam>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="dataGeneratorMetadata">The data generator metadata (used to extract the test class type).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static T GetOrCreate<T>(SharedType sharedType, DataGeneratorMetadata dataGeneratorMetadata, string? key = null) where T : new()
    {
        return GetOrCreate(sharedType, dataGeneratorMetadata, key, static () => new T());
    }

    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type, using the default constructor.
    /// </summary>
    /// <typeparam name="T">The type of instance to get or create. Must have a parameterless constructor.</typeparam>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="testClassType">The test class type (required for <see cref="SharedType.PerClass"/>).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testClassType"/> is null and <paramref name="sharedType"/> is <see cref="SharedType.PerClass"/> or <see cref="SharedType.PerAssembly"/>,
    /// or when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static T GetOrCreate<T>(SharedType sharedType, Type? testClassType = null, string? key = null) where T : new()
    {
        return GetOrCreate(sharedType, testClassType, key, static () => new T());
    }

    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type.
    /// </summary>
    /// <typeparam name="T">The type of instance to get or create.</typeparam>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="testClassType">The test class type (required for <see cref="SharedType.PerClass"/>).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <param name="factory">A factory function to create the instance if not already cached.</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testClassType"/> is null and <paramref name="sharedType"/> is <see cref="SharedType.PerClass"/> or <see cref="SharedType.PerAssembly"/>,
    /// or when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static T GetOrCreate<T>(SharedType sharedType, Type? testClassType, string? key, Func<T> factory)
    {
        _ = factory ?? throw new ArgumentNullException(nameof(factory));

        if (sharedType == SharedType.None)
        {
            return factory();
        }

        return (T)GetShared(sharedType, typeof(T), testClassType, key, () => factory())!;
    }

    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type.
    /// </summary>
    /// <typeparam name="T">The type of instance to get or create.</typeparam>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="dataGeneratorMetadata">The data generator metadata (used to extract the test class type).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <param name="factory">A factory function to create the instance if not already cached.</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static T GetOrCreate<T>(SharedType sharedType, DataGeneratorMetadata dataGeneratorMetadata, string? key, Func<T> factory)
    {
        var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
        return GetOrCreate(sharedType, testClassType, key, factory);
    }

    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type.
    /// </summary>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="type">The type of instance to get or create.</param>
    /// <param name="dataGeneratorMetadata">The data generator metadata (used to extract the test class type).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <param name="factory">A factory function to create the instance if not already cached.</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static object? GetOrCreate(
        SharedType sharedType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type type,
        DataGeneratorMetadata dataGeneratorMetadata,
        string? key,
        Func<object?> factory)
    {
        var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
        return GetOrCreate(sharedType, type, testClassType, key, factory);
    }

    /// <summary>
    /// Gets or creates a shared instance based on the specified sharing type.
    /// </summary>
    /// <param name="sharedType">How the instance should be shared across tests.</param>
    /// <param name="type">The type of instance to get or create.</param>
    /// <param name="testClassType">The test class type (required for <see cref="SharedType.PerClass"/>).</param>
    /// <param name="key">The sharing key (required for <see cref="SharedType.Keyed"/>).</param>
    /// <param name="factory">A factory function to create the instance if not already cached.</param>
    /// <returns>The shared or newly created instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="testClassType"/> is null and <paramref name="sharedType"/> is <see cref="SharedType.PerClass"/> or <see cref="SharedType.PerAssembly"/>,
    /// or when <paramref name="key"/> is null/empty and <paramref name="sharedType"/> is <see cref="SharedType.Keyed"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sharedType"/> is not a valid value.</exception>
    public static object? GetOrCreate(
        SharedType sharedType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type type,
        Type? testClassType,
        string? key,
        Func<object?> factory)
    {
        _ = type ?? throw new ArgumentNullException(nameof(type));
        _ = factory ?? throw new ArgumentNullException(nameof(factory));

        if (sharedType == SharedType.None)
        {
            return factory();
        }

        return GetShared(sharedType, type, testClassType, key, factory);
    }

    /// <summary>
    /// Fetches a shared instance. Inside a running test, the test holds a reference to it until the
    /// test ends, so it is not disposed while the test uses it, and is disposed afterwards if no
    /// other test holds it. If the instance was disposed before the test could hold it, a new one
    /// is created.
    /// </summary>
    private static object? GetShared(SharedType sharedType, Type type, Type? testClassType, string? key, Func<object?> factory)
    {
        var instance = Resolve(sharedType, type, testClassType, key, factory);

        if (instance is null || TestContext.Current is not { } testContext)
        {
            return instance;
        }

        // Fails only when the instance has just been retired, which also evicts it from the cache.
        while (!ObjectTracker.TryHoldForRunningTest(testContext, instance))
        {
            instance = Resolve(sharedType, type, testClassType, key, factory)!;
        }

        return instance;
    }

    private static object? Resolve(SharedType sharedType, Type type, Type? testClassType, string? key, Func<object?> factory)
    {
        return sharedType switch
        {
            SharedType.PerTestSession => TestDataContainer.GetGlobalInstance(type, _ => factory()!),
            SharedType.PerClass => GetForClass(type, testClassType, factory),
            SharedType.Keyed => GetForKey(type, key, factory),
            SharedType.PerAssembly => GetForAssembly(type, testClassType, factory),
            _ => throw new ArgumentOutOfRangeException(nameof(sharedType), sharedType, "Invalid SharedType value.")
        };
    }

    private static object? GetForClass(Type type, Type? testClassType, Func<object?> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerClass.");
        }

        return TestDataContainer.GetInstanceForClass(testClassType, type, _ => factory()!);
    }

    private static object? GetForKey(Type type, string? key, Func<object?> factory)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "key is required when SharedType is Keyed.");
        }

        return TestDataContainer.GetInstanceForKey(key!, type, _ =>
        {
            var instance = factory()!;

            if (instance is IKeyedDataSource keyed)
            {
                keyed.Key = key!;
            }

            return instance;
        });
    }

    private static object? GetForAssembly(Type type, Type? testClassType, Func<object?> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerAssembly.");
        }

        return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, _ => factory()!);
    }
}
