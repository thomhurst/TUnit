using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Engine;

/// <summary>
/// Strongly typed executable test that provides type-safe test execution
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public sealed class ExecutableTest<T> : ExecutableTest where T : class
{
    private TestMetadata<T>? _typedMetadata;
    
    /// <summary>
    /// Strongly typed metadata for this test
    /// </summary>
    public override TestMetadata Metadata 
    { 
        get => _typedMetadata ?? base.Metadata;
        init => _typedMetadata = value as TestMetadata<T> ?? throw new InvalidOperationException($"Expected TestMetadata<{typeof(T).Name}> but got {value?.GetType().Name}");
    }
    
    /// <summary>
    /// Gets the strongly typed metadata
    /// </summary>
    public TestMetadata<T> TypedMetadata => _typedMetadata ?? throw new InvalidOperationException("TypedMetadata not set");
    
    /// <summary>
    /// Factory to create the test class instance
    /// </summary>
    public required Func<Task<T>> CreateTypedInstance { get; init; }
    
    /// <summary>
    /// Strongly typed test invoker
    /// </summary>
    public required Func<T, object?[], CancellationToken, Task> InvokeTypedTest { get; init; }
    
    /// <summary>
    /// Strongly typed property setters
    /// </summary>
    public Dictionary<string, Action<T, object?>> TypedPropertySetters { get; init; } = new();

    /// <inheritdoc/>
    public override async Task<object> CreateInstanceAsync()
    {
        // Properties are now injected during construction by the factory
        var instance = await CreateTypedInstance();
        return instance;
    }

    /// <inheritdoc/>
    public override async Task InvokeTestAsync(object instance, CancellationToken cancellationToken)
    {
        if (instance is not T typedInstance)
        {
            throw new InvalidOperationException($"Expected instance of type {typeof(T).FullName} but got {instance.GetType().FullName}");
        }
        
        // Prepare arguments array - this includes data-driven test arguments
        // but NOT CancellationToken, which is passed separately
        var methodArgs = Arguments;
        
        // Invoke the test with the cancellation token
        await InvokeTypedTest(typedInstance, methodArgs, cancellationToken);
    }
}