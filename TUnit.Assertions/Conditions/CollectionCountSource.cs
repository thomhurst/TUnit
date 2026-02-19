using System.Runtime.CompilerServices;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Provides count assertions that preserve collection type for further chaining.
/// This enables patterns like: Assert.That(list).Count(item => item.IsGreaterThan(3)).IsEqualTo(2).And.Contains(5)
/// </summary>
public class CollectionCountSource<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly AssertionContext<TCollection> _collectionContext;
    private readonly Func<IAssertionSource<TItem>, Assertion<TItem>?>? _assertion;

    public CollectionCountSource(
        AssertionContext<TCollection> collectionContext,
        Func<IAssertionSource<TItem>, Assertion<TItem>?>? assertion)
    {
        _collectionContext = collectionContext;
        _assertion = assertion;
    }

    /// <summary>
    /// Asserts that the count is equal to the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsEqualTo({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.Equal);
    }

    /// <summary>
    /// Asserts that the count is not equal to the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsNotEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsNotEqualTo({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.NotEqual);
    }

    /// <summary>
    /// Asserts that the count is greater than the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsGreaterThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsGreaterThan({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.GreaterThan);
    }

    /// <summary>
    /// Asserts that the count is greater than or equal to the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsGreaterThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsGreaterThanOrEqualTo({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.GreaterThanOrEqual);
    }

    /// <summary>
    /// Asserts that the count is less than the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsLessThan(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsLessThan({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.LessThan);
    }

    /// <summary>
    /// Asserts that the count is less than or equal to the expected value.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsLessThanOrEqualTo(
        int expected,
        [CallerArgumentExpression(nameof(expected))] string? expression = null)
    {
        _collectionContext.ExpressionBuilder.Append($".IsLessThanOrEqualTo({expression})");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, expected, CountComparison.LessThanOrEqual);
    }

    /// <summary>
    /// Asserts that the count is zero.
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsZero()
    {
        _collectionContext.ExpressionBuilder.Append(".IsZero()");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, 0, CountComparison.Equal);
    }

    /// <summary>
    /// Asserts that the count is positive (greater than zero).
    /// Returns a collection-aware assertion that allows further collection chaining.
    /// </summary>
    public CollectionCountEqualsAssertion<TCollection, TItem> IsPositive()
    {
        _collectionContext.ExpressionBuilder.Append(".IsPositive()");
        return new CollectionCountEqualsAssertion<TCollection, TItem>(
            _collectionContext, _assertion, 0, CountComparison.GreaterThan);
    }
}

internal enum CountComparison
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

/// <summary>
/// Collection-aware count assertion that preserves the collection type for further chaining.
/// Inherits from CollectionAssertionBase to enable .And.Contains(), .And.IsNotEmpty(), etc.
/// </summary>
public class CollectionCountEqualsAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<IAssertionSource<TItem>, Assertion<TItem>?>? _itemAssertion;
    private readonly int _expected;
    private readonly CountComparison _comparison;
    private int _actualCount;

    internal CollectionCountEqualsAssertion(
        AssertionContext<TCollection> context,
        Func<IAssertionSource<TItem>, Assertion<TItem>?>? itemAssertion,
        int expected,
        CountComparison comparison)
        : base(context)
    {
        _itemAssertion = itemAssertion;
        _expected = expected;
        _comparison = comparison;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("collection was null");
        }

        // Calculate count
        if (_itemAssertion == null)
        {
            // Simple count without filtering
            _actualCount = value switch
            {
                System.Collections.ICollection c => c.Count,
                _ => System.Linq.Enumerable.Count(value)
            };
        }
        else
        {
            // Count items that satisfy the inner assertion
            _actualCount = 0;
            int index = 0;

            foreach (var item in value)
            {
                var itemAssertionSource = new ValueAssertion<TItem>(item, $"item[{index}]");
                var resultingAssertion = _itemAssertion(itemAssertionSource);

                if (resultingAssertion != null)
                {
                    try
                    {
                        await resultingAssertion.AssertAsync();
                        _actualCount++;
                    }
                    catch
                    {
                        // Item did not satisfy the assertion, don't count it
                    }
                }
                else
                {
                    // Null assertion means no constraint, count all items
                    _actualCount++;
                }

                index++;
            }
        }

        // Check the comparison
        var passed = _comparison switch
        {
            CountComparison.Equal => _actualCount == _expected,
            CountComparison.NotEqual => _actualCount != _expected,
            CountComparison.GreaterThan => _actualCount > _expected,
            CountComparison.GreaterThanOrEqual => _actualCount >= _expected,
            CountComparison.LessThan => _actualCount < _expected,
            CountComparison.LessThanOrEqual => _actualCount <= _expected,
            _ => false
        };

        if (passed)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"received {_actualCount}");
    }

    protected override string GetExpectation()
    {
        var comparisonText = _comparison switch
        {
            CountComparison.Equal => $"to have count equal to {_expected}",
            CountComparison.NotEqual => $"to have count not equal to {_expected}",
            CountComparison.GreaterThan => $"to have count greater than {_expected}",
            CountComparison.GreaterThanOrEqual => $"to have count greater than or equal to {_expected}",
            CountComparison.LessThan => $"to have count less than {_expected}",
            CountComparison.LessThanOrEqual => $"to have count less than or equal to {_expected}",
            _ => $"to have count {_expected}"
        };

        return comparisonText;
    }
}

/// <summary>
/// Collection-aware count assertion that executes an inline count assertion lambda.
/// Preserves the collection type for further chaining.
/// Example: Assert.That(list).Count(c => c.IsEqualTo(5)).And.Contains(1)
/// </summary>
public class CollectionCountWithInlineAssertionAssertion<TCollection, TItem> : CollectionAssertionBase<TCollection, TItem>
    where TCollection : IEnumerable<TItem>
{
    private readonly Func<IAssertionSource<int>, Assertion<int>?> _countAssertion;
    private int _actualCount;

    internal CollectionCountWithInlineAssertionAssertion(
        AssertionContext<TCollection> context,
        Func<IAssertionSource<int>, Assertion<int>?> countAssertion)
        : base(context)
    {
        _countAssertion = countAssertion;
    }

    protected override async Task<AssertionResult> CheckAsync(EvaluationMetadata<TCollection> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return AssertionResult.Failed($"threw {exception.GetType().Name}");
        }

        if (value == null)
        {
            return AssertionResult.Failed("collection was null");
        }

        // Calculate count
        _actualCount = value switch
        {
            System.Collections.ICollection c => c.Count,
            _ => System.Linq.Enumerable.Count(value)
        };

        // Create an assertion source for the count and run the inline assertion
        var countSource = new ValueAssertion<int>(_actualCount, "count");
        var resultingAssertion = _countAssertion(countSource);

        if (resultingAssertion != null)
        {
            try
            {
                await resultingAssertion.AssertAsync();
                return AssertionResult.Passed;
            }
            catch
            {
                // Count assertion failed
                return AssertionResult.Failed($"count was {_actualCount}");
            }
        }

        // Null assertion means no constraint, always pass
        return AssertionResult.Passed;
    }

    protected override string GetExpectation()
    {
        return "to satisfy count assertion";
    }
}
