using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Static entry point for creating mocks.
/// </summary>
public static class Mock
{
    /// <summary>
    /// Retrieves the <see cref="Mock{T}"/> wrapper for a mock implementation object.
    /// Use this to access the mock wrapper from auto-mocked return values or any mocked object.
    /// </summary>
    /// <example>
    /// <code>
    /// var mock = Mock.Of&lt;IServiceA&gt;();
    /// var serviceB = mock.Object.GetServiceB(); // auto-mocked
    /// var autoMock = Mock.Get(serviceB);         // get the wrapper
    /// autoMock.GetValue().Returns(42);
    /// </code>
    /// </example>
    public static Mock<T> Get<T>(T mockedObject) where T : class
    {
        if (mockedObject is IMockObject { MockWrapper: { } wrapper })
        {
            if (wrapper is Mock<T> typed)
            {
                return typed;
            }

            throw new InvalidOperationException(
                $"The object is a mock of '{wrapper.GetType().GenericTypeArguments[0].Name}', not '{typeof(T).Name}'.");
        }

        throw new InvalidOperationException(
            $"The object of type '{typeof(T).Name}' is not a mock. " +
            $"Mock.Get can only be used with objects created by Mock.Of, auto-mocking, or other Mock factory methods.");
    }

    /// <summary>Creates a mock of T in loose mode.</summary>
    public static Mock<T> Of<T>() where T : class => Of<T>(MockBehavior.Loose);

    /// <summary>Creates a mock of T with specified behavior and custom default value provider.</summary>
    public static Mock<T> Of<T>(MockBehavior behavior, IDefaultValueProvider defaultValueProvider) where T : class
    {
        var mock = Of<T>(behavior);
        mock.DefaultValueProvider = defaultValueProvider;
        return mock;
    }

    /// <summary>Creates a mock of T with specified behavior.</summary>
    public static Mock<T> Of<T>(MockBehavior behavior) where T : class
        => Of<T>(behavior, Array.Empty<object>());

    /// <summary>Creates a mock of T in loose mode, optionally passing constructor arguments for concrete classes.</summary>
    public static Mock<T> Of<T>(params object[] constructorArgs) where T : class
        => Of<T>(MockBehavior.Loose, constructorArgs);

    /// <summary>Creates a mock of T with specified behavior, optionally passing constructor arguments for concrete classes.</summary>
    public static Mock<T> Of<T>(MockBehavior behavior, params object[] constructorArgs) where T : class
    {
        if (MockRegistry.TryGetFactory(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior, constructorArgs);
        }

        throw new InvalidOperationException(
            $"No mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced in your project.");
    }

    /// <summary>Creates a delegate mock of T in loose mode.</summary>
    /// <remarks>
    /// Delegate parameters with <c>out</c> modifiers will always receive <c>default</c> values.
    /// Use interface mocks for full <c>out</c> parameter configuration support.
    /// </remarks>
    public static Mock<T> OfDelegate<T>() where T : class => OfDelegate<T>(MockBehavior.Loose);

    /// <summary>Creates a delegate mock of T with specified behavior.</summary>
    /// <remarks>
    /// Delegate parameters with <c>out</c> modifiers will always receive <c>default</c> values.
    /// Use interface mocks for full <c>out</c> parameter configuration support.
    /// </remarks>
    public static Mock<T> OfDelegate<T>(MockBehavior behavior) where T : class
    {
        if (MockRegistry.TryGetDelegateFactory(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior, Array.Empty<object>());
        }

        throw new InvalidOperationException(
            $"No delegate mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced and the type is a delegate type.");
    }

    /// <summary>Creates a wrap mock around an existing instance of T in loose mode.</summary>
    public static Mock<T> Wrap<T>(T instance) where T : class => Wrap(MockBehavior.Loose, instance);

    /// <summary>Creates a wrap mock around an existing instance of T with specified behavior.</summary>
    public static Mock<T> Wrap<T>(MockBehavior behavior, T instance) where T : class
    {
        if (MockRegistry.TryGetWrapFactory(typeof(T), out var factory))
        {
            return (Mock<T>)factory(behavior, instance);
        }

        throw new InvalidOperationException(
            $"No wrap mock factory registered for type '{typeof(T).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced and the type is a non-sealed class with virtual members.");
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
        if (MockRegistry.TryGetMultiFactory(key, out var factory))
        {
            return (Mock<T1>)factory(behavior, Array.Empty<object>());
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}' and '{typeof(T2).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced in your project.");
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
        if (MockRegistry.TryGetMultiFactory(key, out var factory))
        {
            return (Mock<T1>)factory(behavior, Array.Empty<object>());
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}', '{typeof(T2).FullName}', and '{typeof(T3).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced in your project.");
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
        if (MockRegistry.TryGetMultiFactory(key, out var factory))
        {
            return (Mock<T1>)factory(behavior, Array.Empty<object>());
        }

        throw new InvalidOperationException(
            $"No multi-interface mock factory registered for types '{typeof(T1).FullName}', '{typeof(T2).FullName}', '{typeof(T3).FullName}', and '{typeof(T4).FullName}'. " +
            $"Ensure the TUnit.Mocks source generator is referenced in your project.");
    }

    private static string GetMultiKey(params Type[] types)
    {
        return string.Join('|', types.Select(t => t.FullName ?? t.ToString()));
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
