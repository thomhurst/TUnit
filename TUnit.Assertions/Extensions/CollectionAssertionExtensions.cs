using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Extension methods for collection type assertions
/// </summary>
public static class CollectionAssertionExtensions
{
    // === HasSingleItem for IReadOnlyList<T> ===
    public static CustomAssertion<IReadOnlyList<T>> HasSingleItem<T>(this ValueAssertionBuilder<IReadOnlyList<T>> builder)
    {
        return new CustomAssertion<IReadOnlyList<T>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Count == 1;
            },
            "Expected collection to have a single item but had {0} items");
    }

    // === HasSingleItem for IReadOnlyCollection<T> ===
    public static CustomAssertion<IReadOnlyCollection<T>> HasSingleItem<T>(this ValueAssertionBuilder<IReadOnlyCollection<T>> builder)
    {
        return new CustomAssertion<IReadOnlyCollection<T>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.Count == 1;
            },
            "Expected collection to have a single item but had {0} items");
    }

    // === ContainsOnly for IReadOnlyList<T> ===
    public static CustomAssertion<IReadOnlyList<T>> ContainsOnly<T>(this AssertionBuilder<IReadOnlyList<T>> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<IReadOnlyList<T>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                return collection.All(predicate);
            },
            "Expected collection to contain only items matching the predicate");
    }

    // === IsEmpty/IsNotEmpty for any IEnumerable ===
    public static CustomAssertion<TCollection> IsEmpty<TCollection>(this ValueAssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                var enumerator = collection.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            },
            "Expected collection to be empty but it contained items");
    }

    public static CustomAssertion<TCollection> IsNotEmpty<TCollection>(this ValueAssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var enumerator = collection.GetEnumerator();
                try
                {
                    return enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            },
            "Expected collection to not be empty but it was");
    }

    // === Contains for IEnumerable<T> ===
    public static CustomAssertion<TCollection> Contains<TCollection, TElement>(
        this ValueAssertionBuilder<TCollection> builder, TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection => collection != null && collection.Contains(item),
            $"Expected collection to contain {item}");
    }

    public static CustomAssertion<TCollection> DoesNotContain<TCollection, TElement>(
        this ValueAssertionBuilder<TCollection> builder, TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection => collection == null || !collection.Contains(item),
            $"Expected collection to not contain {item}");
    }

    // === HasCount for IEnumerable ===
    public static CustomAssertion<TCollection> HasCount<TCollection>(
        this ValueAssertionBuilder<TCollection> builder, int expectedCount)
        where TCollection : IEnumerable
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return expectedCount == 0;
                int count = 0;
                foreach (var _ in collection)
                {
                    count++;
                }
                return count == expectedCount;
            },
            $"Expected collection to have count {expectedCount}");
    }

    // === IsEquivalentTo for collections ===
    public static CustomAssertion<TCollection> IsEquivalentTo<TCollection, TElement>(
        this ValueAssertionBuilder<TCollection> builder, IEnumerable<TElement> expected)
        where TCollection : IEnumerable<TElement>
        where TElement : notnull
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;

                var actualList = actual.ToList();
                var expectedList = expected.ToList();

                if (actualList.Count != expectedList.Count) return false;

                // Use EqualityComparer to handle nulls properly
                var comparer = EqualityComparer<TElement>.Default;

                // Group and count items
                var expectedGroups = new Dictionary<TElement, int>(comparer);
                foreach (var item in expectedList)
                {
                    if (expectedGroups.ContainsKey(item))
                        expectedGroups[item]++;
                    else
                        expectedGroups[item] = 1;
                }

                var actualGroups = new Dictionary<TElement, int>(comparer);
                foreach (var item in actualList)
                {
                    if (actualGroups.ContainsKey(item))
                        actualGroups[item]++;
                    else
                        actualGroups[item] = 1;
                }

                if (expectedGroups.Count != actualGroups.Count) return false;

                foreach (var kvp in expectedGroups)
                {
                    if (!actualGroups.TryGetValue(kvp.Key, out var count) || count != kvp.Value)
                        return false;
                }
                return true;
            },
            "Expected collection to be equivalent to the expected collection");
    }

    public static CustomAssertion<TCollection> IsNotEquivalentTo<TCollection, TElement>(
        this ValueAssertionBuilder<TCollection> builder, IEnumerable<TElement> expected)
        where TCollection : IEnumerable<TElement>
        where TElement : notnull
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;

                var actualList = actual.ToList();
                var expectedList = expected.ToList();

                if (actualList.Count != expectedList.Count) return true;

                // Use EqualityComparer to handle nulls properly
                var comparer = EqualityComparer<TElement>.Default;

                // Group and count items
                var expectedGroups = new Dictionary<TElement, int>(comparer);
                foreach (var item in expectedList)
                {
                    if (expectedGroups.ContainsKey(item))
                        expectedGroups[item]++;
                    else
                        expectedGroups[item] = 1;
                }

                var actualGroups = new Dictionary<TElement, int>(comparer);
                foreach (var item in actualList)
                {
                    if (actualGroups.ContainsKey(item))
                        actualGroups[item]++;
                    else
                        actualGroups[item] = 1;
                }

                if (expectedGroups.Count != actualGroups.Count) return false;

                foreach (var kvp in expectedGroups)
                {
                    if (!actualGroups.TryGetValue(kvp.Key, out var count) || count != kvp.Value)
                        return true;
                }
                return false;
            },
            "Expected collection to not be equivalent to the expected collection");
    }

    // === For DualAssertionBuilder ===
    public static CustomAssertion<TCollection> IsEmpty<TCollection>(this DualAssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return true;
                var enumerator = collection.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            },
            "Expected collection to be empty but it contained items");
    }

    public static CustomAssertion<TCollection> IsNotEmpty<TCollection>(this DualAssertionBuilder<TCollection> builder)
        where TCollection : IEnumerable
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var enumerator = collection.GetEnumerator();
                try
                {
                    return enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            },
            "Expected collection to not be empty but it was");
    }

    public static CustomAssertion<TCollection> Contains<TCollection, TElement>(
        this DualAssertionBuilder<TCollection> builder, TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection => collection != null && collection.Contains(item),
            $"Expected collection to contain {item}");
    }

    public static CustomAssertion<TCollection> DoesNotContain<TCollection, TElement>(
        this DualAssertionBuilder<TCollection> builder, TElement item)
        where TCollection : IEnumerable<TElement>
    {
        return new CustomAssertion<TCollection>(builder.ActualValueProvider,
            collection => collection == null || !collection.Contains(item),
            $"Expected collection to not contain {item}");
    }
}