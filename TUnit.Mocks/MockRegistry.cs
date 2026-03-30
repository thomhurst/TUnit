using System.Collections.Concurrent;
using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Internal registry for mock factories. Called by generated code at module initialization time.
/// Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockRegistry
{
    // ConcurrentDictionary is used because module initializers from multiple assemblies
    // can run concurrently when test assemblies are loaded in parallel.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object[], object>> _factories = new();

    private static readonly ConcurrentDictionary<string, Func<MockBehavior, object[], object>> _multiFactories = new();

    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object[], object>> _delegateFactories = new();

    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object, object>> _wrapFactories = new();

    /// <summary>
    /// Registers a factory for creating mocks of type T. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterFactory<T>(Func<MockBehavior, object[], Mock<T>> factory) where T : class
    {
        _factories[typeof(T)] = factory;
    }

    /// <summary>
    /// Registers a factory for creating multi-interface mocks. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterMultiFactory(string key, Func<MockBehavior, object[], object> factory)
    {
        _multiFactories[key] = factory;
    }

    /// <summary>
    /// Registers a factory for creating delegate mocks of type T. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterDelegateFactory<T>(Func<MockBehavior, object[], Mock<T>> factory) where T : class
    {
        _delegateFactories[typeof(T)] = factory;
    }

    /// <summary>
    /// Registers a factory for creating wrap mocks of type T. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterWrapFactory<T>(Func<MockBehavior, T, Mock<T>> factory) where T : class
    {
        _wrapFactories[typeof(T)] = (behavior, instance) => factory(behavior, (T)instance);
    }

    /// <summary>
    /// Tries to create an auto-mock for the given type. Returns the mock wrapper (IMock) and
    /// the implementation object. Only works for types with registered factories.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool TryCreateAutoMock(Type type, MockBehavior behavior, out IMock mockWrapper)
    {
        if (_factories.TryGetValue(type, out var factory))
        {
            var mock = (IMock)factory(behavior, Array.Empty<object>());
            mockWrapper = mock;
            return true;
        }

        mockWrapper = null!;
        return false;
    }

    /// <summary>
    /// Gets the mock engine for generated code. Not intended for direct use.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static MockEngine<T> GetEngine<T>(Mock<T> mock) where T : class
        => ((IMockEngineAccess<T>)mock).Engine;

    internal static bool TryGetFactory(Type type, out Func<MockBehavior, object[], object> factory)
        => _factories.TryGetValue(type, out factory!);

    internal static bool TryGetMultiFactory(string key, out Func<MockBehavior, object[], object> factory)
        => _multiFactories.TryGetValue(key, out factory!);

    internal static bool TryGetDelegateFactory(Type type, out Func<MockBehavior, object[], object> factory)
        => _delegateFactories.TryGetValue(type, out factory!);

    internal static bool TryGetWrapFactory(Type type, out Func<MockBehavior, object, object> factory)
        => _wrapFactories.TryGetValue(type, out factory!);
}
