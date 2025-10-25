using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Wrappers;

/// <summary>
/// Wrapper for collection count assertions that provides .EqualTo() method.
/// Example: await Assert.That(list).Count().EqualTo(5);
/// </summary>
public class CountWrapper<TCollection, TItem> : IAssertionSource<TCollection>
    where TCollection : IEnumerable<TItem>
{
    private readonly AssertionContext<TCollection> _context;

    public CountWrapper(AssertionContext<TCollection> context)
    {
        _context = context;
    }

    AssertionContext<TCollection> IAssertionSource<TCollection>.Context => _context;

    /// <summary>
    /// Not supported on CountWrapper - use IsTypeOf on the assertion source before calling HasCount().
    /// </summary>
    TypeOfAssertion<TCollection, TExpected> IAssertionSource<TCollection>.IsTypeOf<TExpected>()
    {
        throw new NotSupportedException(
            "IsTypeOf is not supported after HasCount(). " +
            "Use: Assert.That(value).IsTypeOf<List<int>>().HasCount().EqualTo(5)");
    }

    /// <summary>
    /// Asserts that the collection count is equal to the expected count.
    /// </summary>
    public CollectionCountAssertion<TCollection, TItem> EqualTo(
        int expectedCount,
        [CallerArgumentExpression(nameof(expectedCount))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".EqualTo({expression})");
        return new CollectionCountAssertion<TCollection, TItem>(_context, expectedCount);
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
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

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
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new GreaterThanAssertion<int>(countContext, 0);
    }

    /// <summary>
    /// Asserts that the collection count is greater than the expected count.
    /// </summary>
    public GreaterThanAssertion<int> GreaterThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".GreaterThan({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new GreaterThanAssertion<int>(countContext, expected);
    }

    /// <summary>
    /// Asserts that the collection count is less than the expected count.
    /// </summary>
    public LessThanAssertion<int> LessThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".LessThan({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new LessThanAssertion<int>(countContext, expected);
    }

    /// <summary>
    /// Asserts that the collection count is less than or equal to the expected count.
    /// </summary>
    public LessThanOrEqualAssertion<int> LessThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".LessThanOrEqualTo({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new LessThanOrEqualAssertion<int>(countContext, expected);
    }

    /// <summary>
    /// Asserts that the collection count is between the minimum and maximum values.
    /// </summary>
    public BetweenAssertion<int> Between(
        int minimum,
        int maximum,
        [CallerArgumentExpression(nameof(minimum))] string? minExpression = null,
        [CallerArgumentExpression(nameof(maximum))] string? maxExpression = null)
    {
        _context.ExpressionBuilder.Append($".Between({minExpression}, {maxExpression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new BetweenAssertion<int>(countContext, minimum, maximum);
    }

    /// <summary>
    /// Asserts that the collection count is zero (empty collection).
    /// </summary>
    public CollectionCountAssertion<TCollection, TItem> Zero()
    {
        _context.ExpressionBuilder.Append(".Zero()");
        return new CollectionCountAssertion<TCollection, TItem>(_context, 0);
    }

    /// <summary>
    /// Asserts that the collection count is not equal to the expected count.
    /// </summary>
    public NotEqualsAssertion<int> NotEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".NotEqualTo({expression})");
        // Map context to get the count
        var countContext = _context.Map<int>(value =>
        {
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return value.Cast<object>().Count();
        });
        return new NotEqualsAssertion<int>(countContext, expected);
    }
}
