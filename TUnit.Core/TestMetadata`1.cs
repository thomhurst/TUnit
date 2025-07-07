using System.Linq;
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
    /// Strongly-typed factory delegate that creates an ExecutableTest for this metadata
    /// </summary>
    public Func<ExecutableTestCreationContext, TestMetadata<T>, object>? CreateExecutableTest { get; init; }

    /// <summary>
    /// Backing field for the DataCombinationGenerator property
    /// </summary>
    private Func<IAsyncEnumerable<TestDataCombination>>? _dataCombinationGenerator;

    /// <summary>
    /// Generator delegate that produces all data combinations for this test.
    /// </summary>
    public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator 
    { 
        get
        {
            if (_dataCombinationGenerator != null)
            {
                return _dataCombinationGenerator;
            }
            
            // Return empty enumerable if no generator is set
            return () => EmptyAsyncEnumerable();
        }
    }
    
    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<TestDataCombination> EmptyAsyncEnumerable()
    {
        yield break;
    }
    #pragma warning restore CS1998

    /// <summary>
    /// Factory delegate that creates an ExecutableTest for this metadata.
    /// </summary>
    public override Func<ExecutableTestCreationContext, TestMetadata, object> CreateExecutableTestFactory
    {
        get
        {
            if (CreateExecutableTest != null)
            {
                return (context, metadata) => CreateExecutableTest(context, (TestMetadata<T>)metadata)!;
            }
            
            // Return null to signal that TestBuilder should create a DynamicExecutableTest
            return (context, metadata) => null!;
        }
    }

    /// <summary>
    /// Sets the data combination generator for this test metadata
    /// </summary>
    public void SetDataCombinationGenerator(Func<IAsyncEnumerable<TestDataCombination>> generator)
    {
        _dataCombinationGenerator = generator;
    }
}
