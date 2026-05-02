using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for array values.
/// This is the entry point for: Assert.That(array)
/// Inherits from CollectionAssertionBase&lt;TItem[], TItem&gt; to get IAssertionSource&lt;TItem[]&gt;,
/// enabling generated assertions that target concrete array types (e.g., string[]).
/// </summary>
public class ArrayAssertion<TItem> : CollectionAssertionBase<TItem[], TItem>
#if !NETSTANDARD2_0
    , IAssertionSourceFor<TItem[], ArrayAssertion<TItem>>
#endif
{
    public ArrayAssertion(TItem[]? value, string? expression)
        : base(new AssertionContext<TItem[]>(value!, CreateExpressionBuilder(expression)))
    {
    }

    internal ArrayAssertion(AssertionContext<TItem[]> context)
        : base(context)
    {
    }

#if !NETSTANDARD2_0
    public static ArrayAssertion<TItem> Create(TItem[] item, string label) => new(item, label);
#endif

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
