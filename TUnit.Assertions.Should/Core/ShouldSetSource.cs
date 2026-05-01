using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Should.Core;

public sealed class ShouldSetSource<TItem> : ShouldSetSourceBase<ISet<TItem>, TItem, ShouldSetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldSetSource(ISet<TItem>? value, string? expression)
        : base(new AssertionContext<ISet<TItem>>(value!, BuildExpression(expression)))
    {
    }

    internal ShouldSetSource(AssertionContext<ISet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(ISet<TItem> value) => new SetAdapter<TItem>(value);

    private static StringBuilder BuildExpression(string? expression)
    {
        var sb = new StringBuilder((expression?.Length ?? 1) + 16);
        sb.Append(expression ?? "?").Append(".Should()");
        return sb;
    }
}

#if NET5_0_OR_GREATER
public sealed class ShouldReadOnlySetSource<TItem> : ShouldSetSourceBase<IReadOnlySet<TItem>, TItem, ShouldReadOnlySetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldReadOnlySetSource(IReadOnlySet<TItem>? value, string? expression)
        : base(new AssertionContext<IReadOnlySet<TItem>>(value!, BuildExpression(expression)))
    {
    }

    internal ShouldReadOnlySetSource(AssertionContext<IReadOnlySet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(IReadOnlySet<TItem> value) => new ReadOnlySetAdapter<TItem>(value);

    private static StringBuilder BuildExpression(string? expression)
    {
        var sb = new StringBuilder((expression?.Length ?? 1) + 16);
        sb.Append(expression ?? "?").Append(".Should()");
        return sb;
    }
}
#endif

public sealed class ShouldHashSetSource<TItem> : ShouldSetSourceBase<HashSet<TItem>, TItem, ShouldHashSetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldHashSetSource(HashSet<TItem>? value, string? expression)
        : base(new AssertionContext<HashSet<TItem>>(value!, BuildExpression(expression)))
    {
    }

    internal ShouldHashSetSource(AssertionContext<HashSet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(HashSet<TItem> value) => new SetAdapter<TItem>(value);

    private static StringBuilder BuildExpression(string? expression)
    {
        var sb = new StringBuilder((expression?.Length ?? 1) + 16);
        sb.Append(expression ?? "?").Append(".Should()");
        return sb;
    }
}
