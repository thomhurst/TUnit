using TUnit.Mock.Verification;

namespace TUnit.Mock;

/// <summary>
/// Static entry point for creating mocks.
/// </summary>
public static class Mock
{
    // The source generator registers factories via this method at module initialization time.
    private static readonly Dictionary<Type, Func<MockBehavior, object>> _factories = new();

    // Separate registry for partial mock factories that accept constructor args.
    private static readonly Dictionary<Type, Func<MockBehavior, object[], object>> _partialFactories = new();

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
