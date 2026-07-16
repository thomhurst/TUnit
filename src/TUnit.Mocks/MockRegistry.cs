using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace TUnit.Mocks;

/// <summary>
/// Internal registry for mock factories. Called by generated code at module initialization time.
/// Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockRegistry
{
    private sealed record OpenGenericFactoryDescriptor(
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type OpenImplementationType,
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type? OpenWrapperType);

    // ConcurrentDictionary is used because module initializers from multiple assemblies
    // can run concurrently when test assemblies are loaded in parallel.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object[], object>> _factories = new();
    private static readonly ConcurrentDictionary<Type, OpenGenericFactoryDescriptor> _openGenericFactories = new();

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
    /// Registers an open generic factory descriptor for source-generated generic interface mocks.
    /// Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterOpenGenericFactory(
        Type openGenericType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type openImplementationType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type? openWrapperType)
    {
        _openGenericFactories[openGenericType] = new(openImplementationType, openWrapperType);
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
        if (TryGetFactory(type, out var factory))
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
    {
        if (_factories.TryGetValue(type, out factory!))
            return true;

        if (TryBuildClosedGenericFactory(type, out factory))
        {
            _factories[type] = factory;
            return true;
        }

        return false;
    }

    internal static bool TryGetMultiFactory(string key, out Func<MockBehavior, object[], object> factory)
        => _multiFactories.TryGetValue(key, out factory!);

    internal static bool TryGetDelegateFactory(Type type, out Func<MockBehavior, object[], object> factory)
        => _delegateFactories.TryGetValue(type, out factory!);

    internal static bool TryGetWrapFactory(Type type, out Func<MockBehavior, object, object> factory)
        => _wrapFactories.TryGetValue(type, out factory!);

    private static bool TryBuildClosedGenericFactory(Type closedType, out Func<MockBehavior, object[], object> factory)
    {
        if (!closedType.IsConstructedGenericType)
        {
            factory = null!;
            return false;
        }

        var openType = closedType.GetGenericTypeDefinition();
        if (!_openGenericFactories.TryGetValue(openType, out var descriptor))
        {
            factory = null!;
            return false;
        }

        factory = (behavior, constructorArgs) => CreateClosedGenericMock(closedType, behavior, constructorArgs, descriptor);
        return true;
    }

    private static object CreateClosedGenericMock(Type closedType, MockBehavior behavior, object[] constructorArgs, OpenGenericFactoryDescriptor descriptor)
    {
        if (constructorArgs.Length > 0)
        {
            throw new ArgumentException($"Interface mock '{closedType}' does not support constructor arguments, but {constructorArgs.Length} were provided.");
        }

        var genericArgs = closedType.GetGenericArguments();

#pragma warning disable IL3050, IL2026, IL2055, IL2067
        var engineType = typeof(MockEngine<>).MakeGenericType(closedType);
        var engine = Activator.CreateInstance(engineType, behavior)!;

        var implType = descriptor.OpenImplementationType.MakeGenericType(genericArgs);
        var impl = CreateNonPublicInstance(implType, engine);

        engineType.GetProperty(nameof(MockEngine<object>.Raisable))!.SetValue(engine, impl);

        if (descriptor.OpenWrapperType is not null)
        {
            var wrapperType = descriptor.OpenWrapperType.MakeGenericType(genericArgs);
            return CreateNonPublicInstance(wrapperType, impl, engine);
        }

        var mockType = typeof(Mock<>).MakeGenericType(closedType);
        return Activator.CreateInstance(mockType, impl, engine)!;
#pragma warning restore IL3050, IL2026, IL2055, IL2067
    }

    private static object CreateNonPublicInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type,
        params object?[] args)
    {
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: args,
            culture: CultureInfo.InvariantCulture)!;
    }
}
