#if NET5_0_OR_GREATER
using System.Text;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Entry point for Memory&lt;T&gt; assertions.
/// Provides collection-like assertion methods.
/// </summary>
/// <typeparam name="TItem">The type of items in the memory.</typeparam>
public class MemoryAssertion<TItem> : MemoryAssertionBase<Memory<TItem>, TItem>, IAssertionSource<Memory<TItem>>
{
    /// <summary>
    /// Creates a new MemoryAssertion for the given memory.
    /// </summary>
    public MemoryAssertion(Memory<TItem> value, string expression)
        : base(new AssertionContext<Memory<TItem>>(new EvaluationContext<Memory<TItem>>(value), CreateExpressionBuilder(expression)))
    {
    }

    /// <summary>
    /// Internal constructor for continuation classes.
    /// </summary>
    private protected MemoryAssertion(AssertionContext<Memory<TItem>> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override ICollectionAdapter<TItem> CreateAdapter(Memory<TItem> value) => new MemoryAdapter<TItem>(value);

    /// <inheritdoc />
    AssertionContext<Memory<TItem>> IAssertionSource<Memory<TItem>>.Context => Context;

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}

/// <summary>
/// Entry point for ReadOnlyMemory&lt;T&gt; assertions.
/// Provides collection-like assertion methods.
/// </summary>
/// <typeparam name="TItem">The type of items in the memory.</typeparam>
public class ReadOnlyMemoryAssertion<TItem> : MemoryAssertionBase<ReadOnlyMemory<TItem>, TItem>, IAssertionSource<ReadOnlyMemory<TItem>>
{
    /// <summary>
    /// Creates a new ReadOnlyMemoryAssertion for the given memory.
    /// </summary>
    public ReadOnlyMemoryAssertion(ReadOnlyMemory<TItem> value, string expression)
        : base(new AssertionContext<ReadOnlyMemory<TItem>>(new EvaluationContext<ReadOnlyMemory<TItem>>(value), CreateExpressionBuilder(expression)))
    {
    }

    /// <summary>
    /// Internal constructor for continuation classes.
    /// </summary>
    private protected ReadOnlyMemoryAssertion(AssertionContext<ReadOnlyMemory<TItem>> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override ICollectionAdapter<TItem> CreateAdapter(ReadOnlyMemory<TItem> value) => new ReadOnlyMemoryAdapter<TItem>(value);

    /// <inheritdoc />
    AssertionContext<ReadOnlyMemory<TItem>> IAssertionSource<ReadOnlyMemory<TItem>>.Context => Context;

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
#endif
