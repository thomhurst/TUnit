using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Extensions;

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

    private static int GetCount(TCollection? value) =>
        value is null ? 0
        : value is ICollection collection ? collection.Count
        : value.Cast<object>().Count();

    private AssertionContext<int> MapToCount() => _context.Map<int>(GetCount);

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
    /// Not supported on CountWrapper - use IsAssignableTo on the assertion source before calling HasCount().
    /// </summary>
    IsAssignableToAssertion<TTarget, TCollection> IAssertionSource<TCollection>.IsAssignableTo<TTarget>()
    {
        throw new NotSupportedException(
            "IsAssignableTo is not supported after HasCount(). " +
            "Use: Assert.That(value).IsAssignableTo<IList<int>>().HasCount().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on CountWrapper - use IsNotAssignableTo on the assertion source before calling HasCount().
    /// </summary>
    IsNotAssignableToAssertion<TTarget, TCollection> IAssertionSource<TCollection>.IsNotAssignableTo<TTarget>()
    {
        throw new NotSupportedException(
            "IsNotAssignableTo is not supported after HasCount(). " +
            "Use: Assert.That(value).IsNotAssignableTo<IList<int>>().HasCount().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on CountWrapper - use IsAssignableFrom on the assertion source before calling HasCount().
    /// </summary>
    IsAssignableFromAssertion<TTarget, TCollection> IAssertionSource<TCollection>.IsAssignableFrom<TTarget>()
    {
        throw new NotSupportedException(
            "IsAssignableFrom is not supported after HasCount(). " +
            "Use: Assert.That(value).IsAssignableFrom<IList<int>>().HasCount().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on CountWrapper - use IsNotAssignableFrom on the assertion source before calling HasCount().
    /// </summary>
    IsNotAssignableFromAssertion<TTarget, TCollection> IAssertionSource<TCollection>.IsNotAssignableFrom<TTarget>()
    {
        throw new NotSupportedException(
            "IsNotAssignableFrom is not supported after HasCount(). " +
            "Use: Assert.That(value).IsNotAssignableFrom<IList<int>>().HasCount().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on CountWrapper - use IsNotTypeOf on the assertion source before calling HasCount().
    /// </summary>
    IsNotTypeOfAssertion<TCollection, TExpected> IAssertionSource<TCollection>.IsNotTypeOf<TExpected>()
    {
        throw new NotSupportedException(
            "IsNotTypeOf is not supported after HasCount(). " +
            "Use: Assert.That(value).IsNotTypeOf<List<int>>().HasCount().EqualTo(5)");
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
    public TValue_IsGreaterThanOrEqualTo_TValue_Assertion<int> GreaterThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".GreaterThanOrEqualTo({expression})");
        return new TValue_IsGreaterThanOrEqualTo_TValue_Assertion<int>(MapToCount(), expected);
    }

    /// <summary>
    /// Asserts that the collection count is positive (greater than 0).
    /// </summary>
    public TValue_IsGreaterThan_TValue_Assertion<int> Positive()
    {
        _context.ExpressionBuilder.Append(".Positive()");
        return new TValue_IsGreaterThan_TValue_Assertion<int>(MapToCount(), 0);
    }

    /// <summary>
    /// Asserts that the collection count is greater than the expected count.
    /// </summary>
    public TValue_IsGreaterThan_TValue_Assertion<int> GreaterThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".GreaterThan({expression})");
        return new TValue_IsGreaterThan_TValue_Assertion<int>(MapToCount(), expected);
    }

    /// <summary>
    /// Asserts that the collection count is less than the expected count.
    /// </summary>
    public TValue_IsLessThan_TValue_Assertion<int> LessThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".LessThan({expression})");
        return new TValue_IsLessThan_TValue_Assertion<int>(MapToCount(), expected);
    }

    /// <summary>
    /// Asserts that the collection count is less than or equal to the expected count.
    /// </summary>
    public TValue_IsLessThanOrEqualTo_TValue_Assertion<int> LessThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".LessThanOrEqualTo({expression})");
        return new TValue_IsLessThanOrEqualTo_TValue_Assertion<int>(MapToCount(), expected);
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
        return new BetweenAssertion<int>(MapToCount(), minimum, maximum);
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
        return new NotEqualsAssertion<int>(MapToCount(), expected);
    }
}
