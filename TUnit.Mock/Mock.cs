using System.Collections.Concurrent;
using TUnit.Mock.Verification;

namespace TUnit.Mock;

/// <summary>
/// Static entry point for creating mocks.
/// </summary>
public static class Mock
{
    // The source generator registers factories via this method at module initialization time.
    // ConcurrentDictionary is used because module initializers from multiple assemblies
    // can run concurrently when test assemblies are loaded in parallel.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object>> _factories = new();

    // Separate registry for partial mock factories that accept constructor args.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object[], object>> _partialFactories = new();

    // Registry for multi-interface mock factories, keyed by compound type string.
    private static readonly ConcurrentDictionary<string, Func<MockBehavior, object>> _multiFactories = new();

    // Separate registry for delegate mock factories.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object>> _delegateFactories = new();

    // Separate registry for wrap mock factories that accept a real instance.
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object, object>> _wrapFactories = new();

    /// <summary>
    /// Registers a factory for creating mocks of type T. Called by generated code.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void RegisterFactory<T>(Func<MockBehavior, Mock<T>> factory) where T : class
    {
        _factories[typeof(T)] = behavior => factory(behavior);
    }

    /// <summary>
    /// Registers a factory for creating partial mocks of type T. Called by generated code.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void RegisterPartialFactory<T>(Func<MockBehavior, object[], Mock<T>> factory) where T : class
    {
        _partialFactories[typeof(T)] = (behavior, args) => factory(behavior, args);
    }

    /// <summary>
    /// Registers a factory for creating multi-interface mocks. Called by generated code.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void RegisterMultiFactory(string key, Func<MockBehavior, object> factory)
    {
        _multiFactories[key] = factory;
    }

    /// <summary>
    /// Registers a factory for creating delegate mocks of type T. Called by generated code.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void RegisterDelegateFactory<T>(Func<MockBehavior, Mock<T>> factory) where T : class
    {
        _delegateFactories[typeof(T)] = behavior => factory(behavior);
    }

    /// <summary>
    /// Registers a factory for creating wrap mocks of type T. Called by generated code.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void RegisterWrapFactory<T>(Func<MockBehavior, T, Mock<T>> factory) where T : class
    {
        _wrapFactories[typeof(T)] = (behavior, instance) => factory(behavior, (T)instance);
    }

    /// <summary>Creates a mock of T in loose mode.</summary>
    public static Mock<T> Of<T>() where T : class => Of<T>(MockBehavior.Loose);

    /// <summary>Creates a mock of T with specified behavior.</summary>
    public static Mock<T> Of<T>(MockBehavior behavior) where T : class
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced in your project.");
    }

    /// <summary>Creates a partial mock of T in loose mode, optionally passing constructor arguments.</summary>
    public static Mock<T> OfPartial<T>(params object[] constructorArgs) where T : class
        => OfPartial<T>(MockBehavior.Loose, constructorArgs);

    /// <summary>Creates a partial mock of T with specified behavior, optionally passing constructor arguments.</summary>
    public static Mock<T> OfPartial<T>(MockBehavior behavior, params object[] constructorArgs) where T : class
    {
        if (_partialFactories.TryGetValue(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior, constructorArgs);
        }

        throw new InvalidOperationException(
            $"No partial mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced and the type is not sealed.");
    }

    /// <summary>Creates a delegate mock of T in loose mode.</summary>
    public static Mock<T> OfDelegate<T>() where T : class => OfDelegate<T>(MockBehavior.Loose);

    /// <summary>Creates a delegate mock of T with specified behavior.</summary>
    public static Mock<T> OfDelegate<T>(MockBehavior behavior) where T : class
    {
        if (_delegateFactories.TryGetValue(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No delegate mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced and the type is a delegate type.");
    }

    /// <summary>Creates a wrap mock around an existing instance of T in loose mode.</summary>
    public static Mock<T> Wrap<T>(T instance) where T : class => Wrap(MockBehavior.Loose, instance);

    /// <summary>Creates a wrap mock around an existing instance of T with specified behavior.</summary>
    public static Mock<T> Wrap<T>(MockBehavior behavior, T instance) where T : class
    {
        if (_wrapFactories.TryGetValue(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior, instance);
        }

        throw new InvalidOperationException(
            $"No wrap mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced and the type is a non-sealed class with virtual members.");
    }

    /// <summary>Creates a mock implementing both T1 and T2 in loose mode.</summary>
    public static Mock<T1> Of<T1, T2>()
        where T1 : class where T2 : class
        => Of<T1, T2>(MockBehavior.Loose);

    /// <summary>Creates a mock implementing both T1 and T2 with specified behavior.</summary>
    public static Mock<T1> Of<T1, T2>(MockBehavior behavior)
        where T1 : class where T2 : class
    {
        var key = GetMultiKey(typeof(T1), typeof(T2));
        if (_multiFactories.TryGetValue(key, out var factory))
        {
            return (Mock<T1>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}' and '{typeof(T2).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced in your project.");
    }

    /// <summary>Creates a mock implementing T1, T2, and T3 in loose mode.</summary>
    public static Mock<T1> Of<T1, T2, T3>()
        where T1 : class where T2 : class where T3 : class
        => Of<T1, T2, T3>(MockBehavior.Loose);

    /// <summary>Creates a mock implementing T1, T2, and T3 with specified behavior.</summary>
    public static Mock<T1> Of<T1, T2, T3>(MockBehavior behavior)
        where T1 : class where T2 : class where T3 : class
    {
        var key = GetMultiKey(typeof(T1), typeof(T2), typeof(T3));
        if (_multiFactories.TryGetValue(key, out var factory))
        {
            return (Mock<T1>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}', '{typeof(T2).FullName}', and '{typeof(T3).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced in your project.");
    }

    /// <summary>Creates a mock implementing T1, T2, T3, and T4 in loose mode.</summary>
    public static Mock<T1> Of<T1, T2, T3, T4>()
        where T1 : class where T2 : class where T3 : class where T4 : class
        => Of<T1, T2, T3, T4>(MockBehavior.Loose);

    /// <summary>Creates a mock implementing T1, T2, T3, and T4 with specified behavior.</summary>
    public static Mock<T1> Of<T1, T2, T3, T4>(MockBehavior behavior)
        where T1 : class where T2 : class where T3 : class where T4 : class
    {
        var key = GetMultiKey(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        if (_multiFactories.TryGetValue(key, out var factory))
        {
            return (Mock<T1>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}', '{typeof(T2).FullName}', '{typeof(T3).FullName}', and '{typeof(T4).FullName}'. " +
            $"Ensure the TUnit.Mock source generator is referenced in your project.");
    }

    /// <summary>
    /// Tries to create an auto-mock for the given type. Returns the mock wrapper (IMock) and
    /// the implementation object. Only works for types with registered factories.
    /// Not intended for direct use.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static bool TryCreateAutoMock(Type type, MockBehavior behavior, out IMock mockWrapper)
    {
        if (_factories.TryGetValue(type, out var factory))
        {
            var mock = (IMock)factory(behavior);
            mockWrapper = mock;
            return true;
        }

        mockWrapper = null!;
        return false;
    }

    private static string GetMultiKey(params Type[] types)
    {
        return string.Join("|", types.Select(t => t.FullName));
    }

    /// <summary>
    /// Verifies that the calls described in <paramref name="verificationActions"/>
    /// occurred in the specified order, using global sequence numbers across all mocks.
    /// </summary>
    /// <param name="verificationActions">
    /// An action that invokes <c>mock.Verify.Method(...).WasCalled()</c> in the expected call order.
    /// </param>
    public static void VerifyInOrder(Action verificationActions)
    {
        OrderedVerification.Verify(verificationActions);
    }
}
