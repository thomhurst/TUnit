using System.Text;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Entry point for ISet&lt;T&gt; assertions.
/// Provides set-specific assertion methods like IsSubsetOf, IsSupersetOf, Overlaps, etc.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public class SetAssertion<TItem> : SetAssertionBase<ISet<TItem>, TItem>, IAssertionSource<ISet<TItem>>
{
    /// <summary>
    /// Creates a new SetAssertion for the given set.
    /// </summary>
    public SetAssertion(ISet<TItem> value, string expression)
        : base(new AssertionContext<ISet<TItem>>(new EvaluationContext<ISet<TItem>>(value), CreateExpressionBuilder(expression)))
    {
    }

    /// <summary>
    /// Internal constructor for continuation classes.
    /// </summary>
    private protected SetAssertion(AssertionContext<ISet<TItem>> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override ISetAdapter<TItem> CreateSetAdapter(ISet<TItem> value) => new SetAdapter<TItem>(value);

    /// <inheritdoc />
    AssertionContext<ISet<TItem>> IAssertionSource<ISet<TItem>>.Context => Context;

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}

#if NET5_0_OR_GREATER
/// <summary>
/// Entry point for IReadOnlySet&lt;T&gt; assertions.
/// Provides set-specific assertion methods like IsSubsetOf, IsSupersetOf, Overlaps, etc.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public class ReadOnlySetAssertion<TItem> : SetAssertionBase<IReadOnlySet<TItem>, TItem>, IAssertionSource<IReadOnlySet<TItem>>
{
    /// <summary>
    /// Creates a new ReadOnlySetAssertion for the given read-only set.
    /// </summary>
    public ReadOnlySetAssertion(IReadOnlySet<TItem> value, string expression)
        : base(new AssertionContext<IReadOnlySet<TItem>>(new EvaluationContext<IReadOnlySet<TItem>>(value), CreateExpressionBuilder(expression)))
    {
    }

    /// <summary>
    /// Internal constructor for continuation classes.
    /// </summary>
    private protected ReadOnlySetAssertion(AssertionContext<IReadOnlySet<TItem>> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override ISetAdapter<TItem> CreateSetAdapter(IReadOnlySet<TItem> value) => new ReadOnlySetAdapter<TItem>(value);

    /// <inheritdoc />
    AssertionContext<IReadOnlySet<TItem>> IAssertionSource<IReadOnlySet<TItem>>.Context => Context;

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
#endif

/// <summary>
/// Entry point for HashSet&lt;T&gt; assertions.
/// Provides set-specific assertion methods like IsSubsetOf, IsSupersetOf, Overlaps, etc.
/// </summary>
/// <typeparam name="TItem">The type of items in the set.</typeparam>
public class HashSetAssertion<TItem> : SetAssertionBase<HashSet<TItem>, TItem>, IAssertionSource<HashSet<TItem>>
{
    /// <summary>
    /// Creates a new HashSetAssertion for the given hash set.
    /// </summary>
    public HashSetAssertion(HashSet<TItem> value, string expression)
        : base(new AssertionContext<HashSet<TItem>>(new EvaluationContext<HashSet<TItem>>(value), CreateExpressionBuilder(expression)))
    {
    }

    /// <summary>
    /// Internal constructor for continuation classes.
    /// </summary>
    private protected HashSetAssertion(AssertionContext<HashSet<TItem>> context)
        : base(context)
    {
    }

    /// <inheritdoc />
    protected override ISetAdapter<TItem> CreateSetAdapter(HashSet<TItem> value) => new SetAdapter<TItem>(value);

    /// <inheritdoc />
    AssertionContext<HashSet<TItem>> IAssertionSource<HashSet<TItem>>.Context => Context;

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
