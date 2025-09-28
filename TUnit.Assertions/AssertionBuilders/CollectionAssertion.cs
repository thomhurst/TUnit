using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public enum CollectionAssertType
{
    Empty,
    NotEmpty,
    Count,
    Contains,
    DoesNotContain,
    HasSingleItem,
    HasDistinctItems
}

/// <summary>
///  collection assertion with lazy evaluation
/// </summary>
public class CollectionAssertion<TActual> : AssertionBase<TActual>
{
    private readonly CollectionAssertType _assertType;
    private readonly int? _expectedCount;
    private readonly object? _expectedItem;

    // Internal property to access the actual value provider for chaining extensions
    internal Func<Task<TActual>> ActualValueProvider => GetActualValueAsync;

    public CollectionAssertion(Func<Task<TActual>> actualValueProvider, CollectionAssertType assertType, int? expectedCount = null, object? expectedItem = null)
        : base(actualValueProvider)
    {
        _assertType = assertType;
        _expectedCount = expectedCount;
        _expectedItem = expectedItem;
    }

    public CollectionAssertion(Func<TActual> actualValueProvider, CollectionAssertType assertType, int? expectedCount = null, object? expectedItem = null)
        : base(actualValueProvider)
    {
        _assertType = assertType;
        _expectedCount = expectedCount;
        _expectedItem = expectedItem;
    }

    public CollectionAssertion(TActual actualValue, CollectionAssertType assertType, int? expectedCount = null, object? expectedItem = null)
        : base(actualValue)
    {
        _assertType = assertType;
        _expectedCount = expectedCount;
        _expectedItem = expectedItem;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail("Expected a collection but was null");
        }

        if (!(actual is IEnumerable enumerable))
        {
            return AssertionResult.Fail($"Expected a collection but was {actual.GetType().Name}");
        }

        var items = enumerable.Cast<object?>().ToList();
        var count = items.Count;

        switch (_assertType)
        {
            case CollectionAssertType.Empty:
                if (count == 0)
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected empty collection but had {count} items");

            case CollectionAssertType.NotEmpty:
                if (count > 0)
                    return AssertionResult.Passed;
                return AssertionResult.Fail("Expected non-empty collection but was empty");

            case CollectionAssertType.Count:
                if (count == _expectedCount)
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected {_expectedCount} items but had {count}");

            case CollectionAssertType.Contains:
                if (items.Contains(_expectedItem))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected collection to contain {_expectedItem} but it didn't");

            case CollectionAssertType.DoesNotContain:
                if (!items.Contains(_expectedItem))
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected collection not to contain {_expectedItem} but it did");

            case CollectionAssertType.HasSingleItem:
                if (count == 1)
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected single item but had {count} items");

            case CollectionAssertType.HasDistinctItems:
                var distinctCount = items.Distinct().Count();
                if (distinctCount == count)
                    return AssertionResult.Passed;
                return AssertionResult.Fail($"Expected all items to be distinct but found {count - distinctCount} duplicates");

            default:
                throw new InvalidOperationException($"Unknown assertion type: {_assertType}");
        }
    }
}

/// <summary>
/// Specialized collection assertion that returns the single item for further assertions
/// </summary>
public class SingleItemAssertion<TElement> : AssertionBuilder<TElement>
{
    public SingleItemAssertion(Func<Task<System.Collections.IEnumerable>> collectionProvider)
        : base(async () =>
        {
            var collection = await collectionProvider();
            if (collection == null)
                throw new InvalidOperationException("Collection was null");

            var items = collection.Cast<object?>().ToList();
            if (items.Count != 1)
                throw new InvalidOperationException($"Expected single item but had {items.Count} items");

            return (TElement)items[0]!;
        })
    {
    }
}