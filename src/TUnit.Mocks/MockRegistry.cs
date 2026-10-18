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
        catch (Exception ex) when (!IsDynamicCodeSupported && ex is (NotSupportedException or MemberAccessException or TypeLoadException))
        {
            throw new NotSupportedException(GetNativeAotFallbackMessage(closedType), ex);
        }
    }

    // Attribute-based suppression (not #pragma) so ILC honours it during Native AOT publish. Kept on
    // this method only, so anything added to the caller is still analysed.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Closed types named in source are pre-registered by the generator; on Native AOT any other instantiation that was not pre-compiled fails with a descriptive NotSupportedException.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Open generic implementation/wrapper types are annotated and preserved by RegisterOpenGenericFactory.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only constructs source-generated mock types registered through RegisterOpenGenericFactory.")]
    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Open generic implementation/wrapper types are annotated and preserved by RegisterOpenGenericFactory.")]
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

    // T.Mock() calls the generated closed factory directly, so it works for every type argument,
    // including private nested and file-local types that [GenerateMock] and the generator's
    // closed registrations cannot name from a top-level class.
    internal static string GetNativeAotFallbackMessage(Type closedType)
    {
        var typeName = FormatTypeName(closedType);
        return $"Cannot create a mock of '{typeName}' under Native AOT: the closed generic mock type was not pre-compiled. " +
            $"Create the mock with the closed type written out, preferably {typeName}.Mock(), which calls the generated factory directly. " +
            $"If every type argument is accessible from the assembly, [assembly: GenerateMock(typeof({typeName}))] also registers it ahead of time.";
    }

    /// <summary>
    /// Formats a type as C# source (<c>Ns.Outer&lt;int&gt;.IInner&lt;string&gt;</c>) so the
    /// suggested <c>Mock.Of</c> / <c>GenerateMock</c> fix can be pasted as-is.
    /// With <paramref name="includeNamespace"/> false, namespaces are omitted
    /// (<c>Outer&lt;Int32&gt;.IInner&lt;String&gt;</c>) for shorter diagnostics.
    /// </summary>
    internal static string FormatTypeName(Type type, bool includeNamespace = true)
    {
        if (type.IsArray)
        {
            return $"{FormatTypeName(type.GetElementType()!, includeNamespace)}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        return type.IsGenericType
            ? FormatNamedType(type.GetGenericTypeDefinition(), type.GetGenericArguments(), includeNamespace)
            : FormatNamedType(type, Type.EmptyTypes, includeNamespace);
    }

    // Reflection flattens every type argument of a nested generic onto the innermost type, outer
    // ones first; hand each containing type its share so each segment gets its own argument list.
    private static string FormatNamedType(Type definition, Type[] typeArguments, bool includeNamespace)
    {
        var name = definition.Name;
        var tick = name.IndexOf('`');
        if (tick >= 0)
        {
            name = name.Substring(0, tick);
        }

        string prefix;
        var outerCount = 0;
        if (definition.DeclaringType is { } declaringType)
        {
            outerCount = Math.Min(declaringType.GetGenericArguments().Length, typeArguments.Length);
            prefix = FormatNamedType(declaringType, typeArguments.Take(outerCount).ToArray(), includeNamespace) + ".";
        }
        else
        {
            prefix = !includeNamespace || string.IsNullOrEmpty(definition.Namespace) ? string.Empty : definition.Namespace + ".";
        }

        var ownArguments = typeArguments.Skip(outerCount).ToArray();
        return ownArguments.Length == 0
            ? prefix + name
            : $"{prefix}{name}<{string.Join(", ", ownArguments.Select(a => FormatTypeName(a, includeNamespace)))}>";
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
