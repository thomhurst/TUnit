using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Wrappers;

/// <summary>
/// Wrapper for collection count assertions that provides .EqualTo() method.
/// Example: await Assert.That(list).HasCount().EqualTo(5);
/// </summary>
public class CountWrapper<TValue> : IAssertionSource<TValue>
    where TValue : IEnumerable
{
    private readonly EvaluationContext<TValue> _context;
    private readonly StringBuilder _expressionBuilder;

    public CountWrapper(EvaluationContext<TValue> context, StringBuilder expressionBuilder)
    {
        _context = context;
        _expressionBuilder = expressionBuilder;
    }

    EvaluationContext<TValue> IAssertionSource<TValue>.Context => _context;
    StringBuilder IAssertionSource<TValue>.ExpressionBuilder => _expressionBuilder;

    /// <summary>
    /// Asserts that the collection count is equal to the expected count.
    /// </summary>
    public CollectionCountAssertion<TValue> EqualTo(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        _expressionBuilder.Append($".EqualTo({expression})");
        return new CollectionCountAssertion<TValue>(_context, expectedCount, _expressionBuilder);
    }

    /// <summary>
    /// Asserts that the collection count is greater than or equal to the expected count.
    /// </summary>
    public GreaterThanOrEqualAssertion<int> GreaterThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _expressionBuilder.Append($".GreaterThanOrEqualTo({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null) return 0;
            if (value is System.Collections.ICollection collection)
                return collection.Count;
            return value.Cast<object>().Count();
        });
        return new GreaterThanOrEqualAssertion<int>(countContext, expected, _expressionBuilder);
    }
}
