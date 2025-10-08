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
    private readonly AssertionContext<TValue> _context;

    public CountWrapper(AssertionContext<TValue> context)
    {
        _context = context;
    }

    AssertionContext<TValue> IAssertionSource<TValue>.Context => _context;

    /// <summary>
    /// Asserts that the collection count is equal to the expected count.
    /// </summary>
    public CollectionCountAssertion<TValue> EqualTo(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".EqualTo({expression})");
        return new CollectionCountAssertion<TValue>(_context, expectedCount);
    }

    /// <summary>
    /// Asserts that the collection count is greater than or equal to the expected count.
    /// </summary>
    public GreaterThanOrEqualAssertion<int> GreaterThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".GreaterThanOrEqualTo({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null) return 0;
            if (value is System.Collections.ICollection collection)
                return collection.Count;
            return value.Cast<object>().Count();
        });
        return new GreaterThanOrEqualAssertion<int>(countContext, expected);
    }

    /// <summary>
    /// Asserts that the collection count is positive (greater than 0).
    /// </summary>
    public GreaterThanAssertion<int> Positive()
    {
        _context.ExpressionBuilder.Append(".Positive()");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null) return 0;
            if (value is System.Collections.ICollection collection)
                return collection.Count;
            return value.Cast<object>().Count();
        });
        return new GreaterThanAssertion<int>(countContext, 0);
    }
}
