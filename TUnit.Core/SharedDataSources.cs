using TUnit.Core.Helpers;

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
    public static T GetOrCreate<T>(SharedType sharedType, DataGeneratorMetadata dataGeneratorMetadata, string? key) where T : new()
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
    public static T GetOrCreate<T>(SharedType sharedType, Type? testClassType, string? key) where T : new()
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
        ArgumentNullException.ThrowIfNull(factory);

        return sharedType switch
        {
            SharedType.None => factory(),
            SharedType.PerTestSession => (T)TestDataContainer.GetGlobalInstance(typeof(T), _ => factory()!)!,
            SharedType.PerClass => GetForClass<T>(testClassType, factory),
            SharedType.Keyed => GetForKey<T>(key, factory),
            SharedType.PerAssembly => GetForAssembly<T>(testClassType, factory),
            _ => throw new ArgumentOutOfRangeException(nameof(sharedType), sharedType, "Invalid SharedType value.")
        };
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
    public static object? GetOrCreate(SharedType sharedType, Type type, DataGeneratorMetadata dataGeneratorMetadata, string? key, Func<object?> factory)
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
    public static object? GetOrCreate(SharedType sharedType, Type type, Type? testClassType, string? key, Func<object?> factory)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(factory);

        return sharedType switch
        {
            SharedType.None => factory(),
            SharedType.PerTestSession => TestDataContainer.GetGlobalInstance(type, _ => factory()),
            SharedType.PerClass => GetForClass(type, testClassType, factory),
            SharedType.Keyed => GetForKey(type, key, factory),
            SharedType.PerAssembly => GetForAssembly(type, testClassType, factory),
            _ => throw new ArgumentOutOfRangeException(nameof(sharedType), sharedType, "Invalid SharedType value.")
        };
    }

    private static T GetForClass<T>(Type? testClassType, Func<T> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerClass.");
        }

        return (T)TestDataContainer.GetInstanceForClass(testClassType, typeof(T), _ => factory()!)!;
    }

    private static object? GetForClass(Type type, Type? testClassType, Func<object?> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerClass.");
        }

        return TestDataContainer.GetInstanceForClass(testClassType, type, _ => factory());
    }

    private static T GetForKey<T>(string? key, Func<T> factory)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "key is required when SharedType is Keyed.");
        }

        return (T)TestDataContainer.GetInstanceForKey(key, typeof(T), _ => factory()!)!;
    }

    private static object? GetForKey(Type type, string? key, Func<object?> factory)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "key is required when SharedType is Keyed.");
        }

        return TestDataContainer.GetInstanceForKey(key, type, _ => factory());
    }

    private static T GetForAssembly<T>(Type? testClassType, Func<T> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerAssembly.");
        }

        return (T)TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, typeof(T), _ => factory()!)!;
    }

    private static object? GetForAssembly(Type type, Type? testClassType, Func<object?> factory)
    {
        if (testClassType is null)
        {
            throw new ArgumentNullException(nameof(testClassType), "testClassType is required when SharedType is PerAssembly.");
        }

        return TestDataContainer.GetInstanceForAssembly(testClassType.Assembly, type, _ => factory());
    }
}
