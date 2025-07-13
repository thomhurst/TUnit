using System.Linq;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Strongly typed test metadata that provides type-safe test invocation
/// </summary>
/// <typeparam name="T">The type of the test class</typeparam>
public class TestMetadata<T> : TestMetadata, ITypedTestMetadata where T : class
{
    private Func<object?[], T>? _instanceFactory;
    private Func<T, object?[], Task>? _testInvoker;

    /// <summary>
    /// Strongly typed instance factory
    /// </summary>
    public new Func<object?[], T>? InstanceFactory 
    { 
        get => _instanceFactory;
        init
        {
            _instanceFactory = value;
            // Also set the base class property with a wrapper
            if (value != null)
            {
                base.InstanceFactory = args => value(args);
            }
        }
    }

    /// <summary>
    /// Strongly typed test invoker
    /// </summary>
    public new Func<T, object?[], Task>? TestInvoker 
    { 
        get => _testInvoker;
        init
        {
            _testInvoker = value;
            // Also set the base class property with a wrapper
            if (value != null)
            {
                base.TestInvoker = (instance, args) => value((T)instance, args);
            }
        }
    }

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
    /// Must never be null.
    /// </summary>
    public Func<ExecutableTestCreationContext, TestMetadata<T>, ExecutableTest>? CreateExecutableTest { get; init; }

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
    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            if (CreateExecutableTest != null)
            {
                return (context, metadata) => CreateExecutableTest(context, (TestMetadata<T>)metadata);
            }
            
            throw new InvalidOperationException($"CreateExecutableTest factory is not set for {typeof(T).Name}");
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
