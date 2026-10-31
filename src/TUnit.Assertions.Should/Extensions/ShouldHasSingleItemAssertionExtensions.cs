using System.Collections.Generic;
using TUnit.Assertions.Should.Core;

namespace TUnit.Assertions.Should.Extensions;

public static partial class ShouldHasSingleItemAssertionExtensions
{
    /// <summary>
    /// Lets <c>And</c> and <c>Or</c> continuations on <see cref="IEnumerable{T}"/> call
    /// <c>HaveSingleItem()</c> without type arguments. The generated two-parameter overload
    /// cannot infer <typeparamref name="TItem"/> there, because C# does not infer from constraints.
    /// </summary>
    public static ShouldSingleItemAssertion<IEnumerable<TItem>, TItem> HaveSingleItem<TItem>(
        this IShouldSource<IEnumerable<TItem>> source)
        => source.HaveSingleItem<IEnumerable<TItem>, TItem>();
}
