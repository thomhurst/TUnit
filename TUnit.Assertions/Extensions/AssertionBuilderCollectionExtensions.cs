using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for AssertionBuilder when working with collections
/// This allows collection methods to be available after And/Or chains
/// </summary>
public static class AssertionBuilderCollectionExtensions
{
    // Specific overloads for IEnumerable<T> to help with type inference
    public static CustomAssertion<IEnumerable<TElement>> IsEmpty<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return !collection.Any();
            },
            "Expected collection to be empty but it contained items");
    }

    public static CustomAssertion<IEnumerable<TElement>> IsNotEmpty<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Any();
            },
            "Expected collection to be non-empty but it was empty");
    }

    public static CustomAssertion<IEnumerable<TElement>> HasSingleItem<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == 1;
            },
            "Expected collection to have a single item but had {{0}} items");
    }

    public static CustomAssertion<IEnumerable<TElement>> HasDistinctItems<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                var list = collection.ToList();
                return list.Count == list.Distinct().Count();
            },
            "Expected collection to have distinct items but found duplicates");
    }

    public static CustomAssertion<TCollection> IsEmpty<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return !collection.Any();
            },
            "Expected collection to be empty but it contained items");
    }

    public static CustomAssertion<TCollection> IsNotEmpty<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Any();
            },
            "Expected collection to be non-empty but it was empty");
    }

    // Overload that returns a count assertion builder for chaining
    public static ValueAssertionBuilder<int> HasCount<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder)
    {
        return new ValueAssertionBuilder<int>(async () =>
        {
            var collection = await builder.ActualValueProvider();
            if (collection == null)
                throw new InvalidOperationException("Collection was null");
            return collection.Count();
        });
    }

    public static CustomAssertion<IEnumerable<TElement>> HasCount<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder,
        int expectedCount)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == expectedCount;
            },
            $"Expected collection to have {expectedCount} items but had {{0}} items");
    }

    public static CustomAssertion<TCollection> HasCount<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder,
        int expectedCount)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == expectedCount;
            },
            $"Expected collection to have {expectedCount} items but had {{0}} items");
    }

    public static CustomAssertion<TCollection> Contains<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder,
        TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Contains(item);
            },
            $"Expected collection to contain {item} but it didn't");
    }

    public static CustomAssertion<TCollection> DoesNotContain<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder,
        TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                return !collection.Contains(item);
            },
            $"Expected collection not to contain {item} but it did");
    }

    // Specific overload for IEnumerable<T> to help with type inference
    public static CustomAssertion<IEnumerable<TElement>> ContainsOnly<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder,
        Func<TElement, bool> predicate)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                return actual.All(predicate);
            },
            "Expected collection to contain only items matching the predicate");
    }

    public static CustomAssertion<TCollection> ContainsOnly<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder,
        Func<TElement, bool> predicate)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                return actual.All(predicate);
            },
            "Expected collection to contain only items matching the predicate");
    }

    // Specific overload for IEnumerable<T> to help with type inference
    public static CustomAssertion<IEnumerable<TElement>> DoesNotContain<TElement>(
        this AssertionBuilder<IEnumerable<TElement>> builder,
        Func<TElement, bool> predicate)
    {
        return new CustomAssertion<IEnumerable<TElement>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                return !actual.Any(predicate);
            },
            "Expected collection to not contain items matching the predicate");
    }

    public static CustomAssertion<TCollection> DoesNotContain<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder,
        Func<TElement, bool> predicate)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                return !actual.Any(predicate);
            },
            "Expected collection to not contain items matching the predicate");
    }

    public static CustomAssertion<TCollection> HasSingleItem<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var count = collection.Count();
                return count == 1;
            },
            "Expected collection to have a single item but had {{0}} items");
    }

    public static CustomAssertion<TCollection> HasDistinctItems<TCollection, TElement>(
        this AssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                var list = collection.ToList();
                return list.Count == list.Distinct().Count();
            },
            "Expected collection to have distinct items but found duplicates");
    }
}