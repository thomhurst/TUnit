using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion builder for collection types
/// Provides both value assertions (from base class) AND collection-specific assertions
/// </summary>
public class CollectionAssertionBuilder<TCollection, TElement> : ValueAssertionBuilder<TCollection>
    where TCollection : IEnumerable<TElement>
{

    public CollectionAssertionBuilder(TCollection value, string? expression = null)
        : base(value, expression)
    {
    }

    public CollectionAssertionBuilder(Func<TCollection> valueProvider, string? expression = null)
        : base(valueProvider, expression)
    {
    }

    public CollectionAssertionBuilder(Func<Task<TCollection>> asyncValueProvider, string? expression = null)
        : base(asyncValueProvider, expression)
    {
    }

    public CollectionAssertionBuilder(Task<TCollection> task, string? expression = null)
        : base(task, expression)
    {
    }

    public CollectionAssertionBuilder(ValueTask<TCollection> valueTask, string? expression = null)
        : base(valueTask, expression)
    {
    }

    // Collection-specific assertions
    public CustomAssertion<TCollection> IsEmpty()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return false; // null is not empty
                return !collection.Any();
            },
            "Expected collection to be empty but it contained items");
    }

    // All() returns a CollectionAllAssertion for chaining with Satisfy
    public CollectionAllAssertion<TElement> All()
    {
        return new CollectionAllAssertion<TElement>(async () => await _actualValueProvider());
    }

    // Predicate assertions for collections
    public CustomAssertion<TCollection> All(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection => collection != null && collection.All(predicate),
            "Expected all elements in collection to match the predicate");
    }

    public CustomAssertion<TCollection> Any(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection => collection != null && collection.Any(predicate),
            "Expected at least one element in collection to match the predicate");
    }

    public CustomAssertion<TCollection> None(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection => collection == null || !collection.Any(predicate),
            "Expected no elements in collection to match the predicate");
    }

    public CustomAssertion<TCollection> IsNotEmpty()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Any();
            },
            "Expected collection to be non-empty but it was empty");
    }

    public CustomAssertion<TCollection> HasCount(int expectedCount)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == expectedCount;
            },
            $"Expected collection to have {expectedCount} items but had {{0}} items");
    }

    // HasCount that returns an AssertionBuilder<int> for further chaining
    public ValueAssertionBuilder<int> HasCount()
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var actual = await _actualValueProvider();
            if (actual == null)
                throw new InvalidOperationException("Collection was null");
            return actual.Count();
        });
    }

    public CustomAssertion<TCollection> Contains(TElement item)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Contains(item);
            },
            $"Expected collection to contain {item} but it didn't");
    }

    // Contains with predicate - returns assertion that yields the found item
    public ContainsPredicateAssertion<TCollection, TElement> Contains(Func<TElement, bool> predicate)
    {
        return new ContainsPredicateAssertion<TCollection, TElement>(_actualValueProvider, predicate);
    }

    public CustomAssertion<TCollection> DoesNotContain(TElement item)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                return !collection.Contains(item);
            },
            $"Expected collection not to contain {item} but it did");
    }

    // DoesNotContain with predicate
    public CustomAssertion<TCollection> DoesNotContain(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                return !actual.Any(predicate);
            },
            "Expected collection to not contain items matching the predicate");
    }

    // ContainsOnly with predicate
    public CustomAssertion<TCollection> ContainsOnly(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                return actual.All(predicate);
            },
            "Expected collection to contain only items matching the predicate");
    }

    public CustomAssertion<TCollection> HasSingleItem()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == 1;
            },
            "Expected collection to have a single item but had {{0}} items");
    }

    public async Task<TElement> GetSingleItem()
    {
        var collection = await _actualValueProvider();
        if (collection == null)
            throw new InvalidOperationException("Collection was null");

        var list = collection.ToList();
        if (list.Count != 1)
            throw new InvalidOperationException($"Expected single item but had {list.Count} items");

        return list[0];
    }

    public CustomAssertion<TCollection> HasDistinctItems()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                var list = collection.ToList();
                return list.Count == list.Distinct().Count();
            },
            "Expected collection to have distinct items but found duplicates");
    }

    // Collection ordering assertions
    public CustomAssertion<TCollection> IsInOrder()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var list = actual.ToList();
                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (Comparer<TElement>.Default.Compare(list[i], list[i + 1]) > 0)
                        return false;
                }
                return true;
            },
            "Expected collection to be in ascending order");
    }

    public CustomAssertion<TCollection> IsInDescendingOrder()
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                var list = actual.ToList();
                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (Comparer<TElement>.Default.Compare(list[i], list[i + 1]) < 0)
                        return false;
                }
                return true;
            },
            "Expected collection to be in descending order");
    }

    // Collection equivalence - same items, any order
    public CustomAssertion<TCollection> IsEquivalentTo(IEnumerable<TElement> expected)
    {
        return new CustomAssertion<TCollection>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                var actualList = actual.ToList();
                var expectedList = expected.ToList();
                return actualList.Count == expectedList.Count &&
                       actualList.All(expectedList.Contains) &&
                       expectedList.All(actualList.Contains);
            },
            $"Expected collection to be equivalent to the expected collection");
    }

    // Implicit conversion support for awaiting
    public new TaskAwaiter<TCollection> GetAwaiter()
    {
        return _actualValueProvider().GetAwaiter();
    }

    // Base object methods overrides to prevent accidental usage
    [Obsolete("This is a base `object` method that should not be called.", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}