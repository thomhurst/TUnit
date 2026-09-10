using System.Collections.Generic;
using System.ComponentModel;
using TUnit.Assertions.Adapters;
using TUnit.Assertions.Abstractions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should.Attributes;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Should.Core;

[ShouldGeneratePartial(typeof(SetAssertion<>))]
public sealed partial class ShouldSetSource<TItem> : ShouldSetSourceBase<ISet<TItem>, TItem, ShouldSetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldSetSource(ISet<TItem>? value, string? expression)
        : base(new AssertionContext<ISet<TItem>>(value!, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldSetSource(AssertionContext<ISet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(ISet<TItem> value) => new SetAdapter<TItem>(value);
}

#if NET5_0_OR_GREATER
[ShouldGeneratePartial(typeof(ReadOnlySetAssertion<>))]
public sealed partial class ShouldReadOnlySetSource<TItem> : ShouldSetSourceBase<IReadOnlySet<TItem>, TItem, ShouldReadOnlySetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldReadOnlySetSource(IReadOnlySet<TItem>? value, string? expression)
        : base(new AssertionContext<IReadOnlySet<TItem>>(value!, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldReadOnlySetSource(AssertionContext<IReadOnlySet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(IReadOnlySet<TItem> value) => new ReadOnlySetAdapter<TItem>(value);
}
#endif

[ShouldGeneratePartial(typeof(HashSetAssertion<>))]
public sealed partial class ShouldHashSetSource<TItem> : ShouldSetSourceBase<HashSet<TItem>, TItem, ShouldHashSetSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldHashSetSource(HashSet<TItem>? value, string? expression)
        : base(new AssertionContext<HashSet<TItem>>(value!, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldHashSetSource(AssertionContext<HashSet<TItem>> context)
        : base(context)
    {
    }

    protected override ISetAdapter<TItem> CreateSetAdapter(HashSet<TItem> value) => new SetAdapter<TItem>(value);
}
