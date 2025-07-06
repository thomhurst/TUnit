using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Strongly typed test metadata that provides type-safe test invocation
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public class TestMetadata<T> : TestMetadata, ITypedTestMetadata where T : class
{
    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<object[], T>? InstanceFactory { get; init; }

    /// <summary>
    /// Strongly typed test invoker
    /// </summary>
    public new Func<T, object?[], Task>? TestInvoker { get; init; }

    /// <summary>
    /// Property setters for this test class
    /// </summary>
    public new Dictionary<string, Action<T, object?>> PropertySetters { get; init; } = new();

    /// <summary>
    /// Typed instance creator for ExecutableTest<T>
    /// </summary>
    public Func<Task<T>>? CreateTypedInstance { get; init; }

    /// <summary>
    /// Typed test invoker for ExecutableTest<T> with CancellationToken support
    /// </summary>
    public Func<T, object?[], CancellationToken, Task>? InvokeTypedTest { get; init; }

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata
    /// </summary>
    public Func<ExecutableTestCreationContext, TestMetadata<T>, object>? CreateExecutableTest { get; init; }
}
