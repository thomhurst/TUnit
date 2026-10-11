using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks;

/// <summary>
/// Internal registry for mock factories. Called by generated code at module initialization time.
/// Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockRegistry
{
    private sealed record OpenGenericFactoryDescriptor(
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type OpenImplementationType,
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
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

    // Fallback for closed generic interfaces the source generator did not see written out (e.g.
    // Mock.Of<T>() inside generic code, or an auto-mocked return type). Closed types named at a
    // Mock.Of call site or in [GenerateMock] get a static closed factory instead (#7027).
    // Attribute-based suppression (not #pragma) so ILC honours it during Native AOT publish.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Closed types named in source are pre-registered by the generator; on Native AOT any other instantiation that was not pre-compiled fails with a descriptive NotSupportedException.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Open generic implementation/wrapper types are annotated and preserved by RegisterOpenGenericFactory.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only constructs source-generated mock types registered through RegisterOpenGenericFactory.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Open generic implementation/wrapper types are annotated and preserved by RegisterOpenGenericFactory.")]
    private static object CreateClosedGenericMock(Type closedType, MockBehavior behavior, object[] constructorArgs, OpenGenericFactoryDescriptor descriptor)
    {
        if (constructorArgs.Length > 0)
        {
            throw new ArgumentException($"Interface mock '{closedType}' does not support constructor arguments, but {constructorArgs.Length} were provided.");
        }

        try
        {
            return CreateClosedGenericMockCore(closedType, behavior, descriptor);
        }
        // Native AOT reports a missing instantiation as NotSupportedException or a
        // MemberAccessException-derived missing-artifact exception, depending on the version.
        catch (Exception ex) when (!IsDynamicCodeSupported && ex is NotSupportedException or MemberAccessException or TypeLoadException)
        {
            throw new NotSupportedException(
                $"Cannot create a mock of '{closedType}' under Native AOT: the closed generic mock type was not pre-compiled. " +
                $"Name the closed type at a call site (Mock.Of<{FormatTypeName(closedType)}>()) or add [assembly: GenerateMock(typeof({FormatTypeName(closedType)}))] so the source generator registers it ahead of time.",
                ex);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "See CreateClosedGenericMock.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "See CreateClosedGenericMock.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "See CreateClosedGenericMock.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "See CreateClosedGenericMock.")]
    private static object CreateClosedGenericMockCore(Type closedType, MockBehavior behavior, OpenGenericFactoryDescriptor descriptor)
    {
        var genericArgs = closedType.GetGenericArguments();

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
    }

#if NET
    private static bool IsDynamicCodeSupported => RuntimeFeature.IsDynamicCodeSupported;
#else
    // netstandard2.0 consumers never run on Native AOT.
    private static bool IsDynamicCodeSupported => true;
#endif

    private static string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
        {
            name = name.Substring(0, tick);
        }

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatTypeName))}>";
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
