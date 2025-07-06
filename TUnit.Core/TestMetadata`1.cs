namespace TUnit.Core;

/// <summary>
/// Strongly typed test metadata that provides type-safe test invocation
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public class TestMetadata<T> : TestMetadata where T : class
{
    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<object[], T>? InstanceFactory { get; init; }

    /// <summary>
    /// Strongly typed test invoker that can handle CancellationToken and other parameters
    /// </summary>
    public new Func<T, object?[], CancellationToken, Task>? TestInvoker { get; init; }

    /// <summary>
    /// Property setters for this test class
    /// </summary>
    public new Dictionary<string, Action<T, object?>> PropertySetters { get; init; } = new();
}
