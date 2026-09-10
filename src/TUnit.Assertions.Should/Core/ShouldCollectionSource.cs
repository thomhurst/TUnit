using System.ComponentModel;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Should.Attributes;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Should.Core;

/// <summary>
/// Should-flavored entry wrapper for collections. Exposes element-typed instance methods
/// (<c>BeInOrder</c>/<c>All</c>/<c>HaveSingleItem</c>) so callers don't need explicit type
/// arguments — the generated extension form
/// <c>Method&lt;TCollection, TItem&gt;(IShouldSource&lt;TCollection&gt;)</c> can't infer
/// <c>TItem</c> from a constraint alone.
/// <para>
/// The Should-flavored instance methods are emitted by the source generator from
/// <see cref="CollectionAssertion{TItem}"/>'s public instance methods — see
/// <see cref="ShouldGeneratePartialAttribute{T}"/>.
/// </para>
/// </summary>
[ShouldGeneratePartial(typeof(CollectionAssertion<>))]
public sealed partial class ShouldCollectionSource<TItem> : ShouldEnumerableSourceBase<IEnumerable<TItem>, TItem, ShouldCollectionSource<TItem>>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ShouldCollectionSource(IEnumerable<TItem>? value, string? expression)
        : base(new AssertionContext<IEnumerable<TItem>>(value, ShouldExpressionBuilder.Build(expression)))
    {
    }

    internal ShouldCollectionSource(AssertionContext<IEnumerable<TItem>> context)
        : base(context)
    {
    }
}
