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
{
    public ArrayAssertion(TItem[]? value, string? expression)
        : base(new AssertionContext<TItem[]>(value!, CreateExpressionBuilder(expression)))
    {
    }

    internal ArrayAssertion(AssertionContext<TItem[]> context)
        : base(context)
    {
    }

    private static StringBuilder CreateExpressionBuilder(string? expression)
    {
        var builder = new StringBuilder();
        builder.Append($"Assert.That({expression ?? "?"})");
        return builder;
    }
}
