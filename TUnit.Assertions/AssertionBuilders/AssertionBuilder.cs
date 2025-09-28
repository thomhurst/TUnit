using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

public class AssertionBuilder<TActual>
{
    private readonly Func<Task<TActual>> _actualValueProvider;
    private readonly string? _expression;

    internal Func<Task<TActual>> ActualValueProvider => _actualValueProvider;

    // Fluent chaining property - returns self for readability
    public AssertionBuilder<TActual> And => this;

    // Support for awaiting the assertion builder to get the actual value
    public TaskAwaiter<TActual> GetAwaiter()
    {
        return _actualValueProvider().GetAwaiter();
    }

    public AssertionBuilder(TActual value, string? expression = null)
        : this(() => Task.FromResult(value), expression)
    {
    }

    public AssertionBuilder(Func<TActual> valueProvider, string? expression = null)
        : this(() => Task.FromResult(valueProvider()), expression)
    {
    }

    public AssertionBuilder(Func<Task<TActual>> asyncValueProvider, string? expression = null)
    {
        _actualValueProvider = asyncValueProvider;
        _expression = expression;
    }

    public AssertionBuilder(Task<TActual> task, string? expression = null)
        : this(() => task, expression)
    {
    }

    public AssertionBuilder(ValueTask<TActual> valueTask, string? expression = null)
        : this(() => valueTask.AsTask(), expression)
    {
    }

    // Equality assertions
    public GenericEqualToAssertion<TActual> IsEqualTo(TActual expected)
    {
        return new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

    // Overload with custom message for backward compatibility
    public GenericEqualToAssertion<TActual> IsEqualTo(TActual expected, string because)
    {
        var assertion = new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
        assertion.Because(because);
        return assertion;
    }

    // Overload for cross-type comparisons
    public CustomAssertion<TActual> IsEqualTo(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => object.Equals(actual, expected),
            $"Expected {expected} but was {{ActualValue}}");
    }

    // Overload with custom equality comparer
    public CustomAssertion<TActual> IsEqualTo(TActual expected, IEqualityComparer<TActual> comparer)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => comparer.Equals(actual, expected),
            $"Expected {expected} using custom comparer but was {{ActualValue}}");
    }

    // Alias for IsEqualTo
    public GenericEqualToAssertion<TActual> EqualTo(TActual expected)
    {
        return IsEqualTo(expected);
    }

    // Alias for IsEqualTo for backwards compatibility
    public GenericEqualToAssertion<TActual> IsEquatableOrEqualTo(TActual expected)
    {
        return new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

    public GenericNotEqualToAssertion<TActual> IsNotEqualTo(TActual expected)
    {
        return new GenericNotEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

    // Null assertions
    public NullAssertion<TActual> IsNull()
    {
        return new NullAssertion<TActual>(_actualValueProvider, shouldBeNull: true);
    }

    public NullAssertion<TActual> IsNotNull()
    {
        return new NullAssertion<TActual>(_actualValueProvider, shouldBeNull: false);
    }

    // Comparable assertions
    public ComparisonAssertion<TActual> IsGreaterThan(TActual value)
    {
        return new ComparisonAssertion<TActual>(_actualValueProvider, value, ComparisonType.GreaterThan);
    }

    // Convenient method for checking if a numeric value is positive (greater than zero)
    public ComparisonAssertion<TActual> IsPositive()
    {
        // Assumes TActual can be compared to default(TActual) which is 0 for numeric types
        return IsGreaterThan(default!);
    }

    // Convenient method for checking if a numeric value is negative (less than zero)
    public ComparisonAssertion<TActual> IsNegative()
    {
        // Assumes TActual can be compared to default(TActual) which is 0 for numeric types
        return IsLessThan(default!);
    }

    public ComparisonAssertion<TActual> IsGreaterThanOrEqualTo(TActual value)
    {
        return new ComparisonAssertion<TActual>(_actualValueProvider, value, ComparisonType.GreaterThanOrEqual);
    }

    // Alias for backwards compatibility
    public ComparisonAssertion<TActual> GreaterThanOrEqualTo(TActual value)
    {
        return IsGreaterThanOrEqualTo(value);
    }

    public ComparisonAssertion<TActual> IsLessThan(TActual value)
    {
        return new ComparisonAssertion<TActual>(_actualValueProvider, value, ComparisonType.LessThan);
    }

    public ComparisonAssertion<TActual> IsLessThanOrEqualTo(TActual value)
    {
        return new ComparisonAssertion<TActual>(_actualValueProvider, value, ComparisonType.LessThanOrEqual);
    }

    // Boolean assertions (when TActual is bool)
    public BooleanAssertion IsTrue()
    {
        if (typeof(TActual) != typeof(bool))
            throw new InvalidOperationException("IsTrue can only be used with boolean values");

        var boolProvider = async () => {
            var value = await _actualValueProvider();
            return (bool)(object)value!;
        };
        return new BooleanAssertion(boolProvider, expectedValue: true);
    }

    public BooleanAssertion IsFalse()
    {
        if (typeof(TActual) != typeof(bool))
            throw new InvalidOperationException("IsFalse can only be used with boolean values");

        var boolProvider = async () => {
            var value = await _actualValueProvider();
            return (bool)(object)value!;
        };
        return new BooleanAssertion(boolProvider, expectedValue: false);
    }

    // Type assertions
    public TypeAssertion<TActual> IsTypeOf(Type expectedType)
    {
        return new TypeAssertion<TActual>(_actualValueProvider, expectedType, exact: true);
    }

    public TypeAssertion<TActual> IsAssignableTo(Type expectedType)
    {
        return new TypeAssertion<TActual>(_actualValueProvider, expectedType, exact: false);
    }

    // Collection assertions (when TActual is IEnumerable)
    public CollectionAssertion<TActual> IsEmpty()
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.Empty);
    }

    public CollectionAssertion<TActual> IsNotEmpty()
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.NotEmpty);
    }

    public CollectionAssertion<TActual> HasCount(int expectedCount)
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.Count, expectedCount);
    }

    // HasCount that returns an AssertionBuilder<int> for further chaining
    public AssertionBuilder<int> HasCount()
    {
        return new AssertionBuilder<int>(async () =>
        {
            var actual = await _actualValueProvider();
            if (actual == null)
                throw new InvalidOperationException("Collection was null");
            if (!(actual is System.Collections.IEnumerable enumerable))
                throw new InvalidOperationException($"Expected a collection but was {actual.GetType().Name}");
            return enumerable.Cast<object?>().Count();
        });
    }

    public CollectionAssertion<TActual> Contains(object? item)
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.Contains, expectedItem: item);
    }

    // Contains with predicate - for collections, returns assertion that yields the found item
    // Note: This is a general version, specific typed versions are provided as extension methods
    public ContainsPredicateAssertion<IEnumerable<TItem>, TItem> Contains<TItem>(Func<TItem, bool> predicate)
    {
        // Cast the actual value provider to work with IEnumerable<TItem>
        Func<Task<IEnumerable<TItem>>> collectionProvider = async () =>
        {
            var actual = await _actualValueProvider();
            if (actual is IEnumerable<TItem> collection)
                return collection;
            throw new InvalidOperationException($"Value of type {typeof(TActual).Name} is not IEnumerable<{typeof(TItem).Name}>");
        };
        return new ContainsPredicateAssertion<IEnumerable<TItem>, TItem>(collectionProvider, predicate);
    }

    // Contains overload for object collections to help with type inference
    public CustomAssertion<TActual> Contains(Func<object?, bool> predicate)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                if (!(actual is System.Collections.IEnumerable enumerable)) return false;
                foreach (var item in enumerable)
                {
                    if (predicate(item)) return true;
                }
                return false;
            },
            "Expected collection to contain an item matching the predicate");
    }

    public CollectionAssertion<TActual> DoesNotContain(object? item)
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.DoesNotContain, expectedItem: item);
    }

    // DoesNotContain with predicate
    public CustomAssertion<TActual> DoesNotContain<TElement>(Func<TElement, bool> predicate)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return true;
                if (actual is IEnumerable<TElement> enumerable)
                {
                    return !enumerable.Any(predicate);
                }
                return true;
            },
            "Expected collection to not contain items matching the predicate");
    }

    // Note: For collections, prefer using the extension methods in CollectionExtensions that return the single item
    // This method does not return the item, only asserts that there is a single item
    public CollectionAssertion<TActual> AssertHasSingleItem()
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.HasSingleItem);
    }


    public CollectionAssertion<TActual> HasDistinctItems()
    {
        return new CollectionAssertion<TActual>(_actualValueProvider, CollectionAssertType.HasDistinctItems);
    }

    // Collection equivalence - same items, any order
    public CustomAssertion<TActual> IsEquivalentTo(TActual expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                if (!(actual is System.Collections.IEnumerable actualEnumerable)) return false;
                if (!(expected is System.Collections.IEnumerable expectedEnumerable)) return false;
                var actualList = actualEnumerable.Cast<object?>().ToList();
                var expectedList = expectedEnumerable.Cast<object?>().ToList();
                return actualList.Count == expectedList.Count &&
                       actualList.All(expectedList.Contains) &&
                       expectedList.All(actualList.Contains);
            },
            $"Expected collection to be equivalent to the expected collection");
    }

    // IsEquivalentTo for object (handles anonymous types and cross-type comparisons)
    public CustomAssertion<TActual> IsEquivalentTo(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                // For now, just use equals - could be enhanced with deep property comparison
                return actual.Equals(expected);
            },
            $"Expected {{0}} to be equivalent to {expected}");
    }

    public CustomAssertion<TActual> IsEquivalentTo(IEnumerable<object?> expected)
    {
        var expectedList = expected?.ToList() ?? new List<object?>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                if (!(actual is System.Collections.IEnumerable actualEnumerable)) return false;
                var actualList = actualEnumerable.Cast<object?>().ToList();
                return actualList.Count == expectedList.Count &&
                       actualList.All(expectedList.Contains) &&
                       expectedList.All(actualList.Contains);
            },
            $"Expected collection to be equivalent to the expected collection");
    }

    // IsEquivalentTo with custom comparer - generic overload
    public CustomAssertion<TActual> IsEquivalentTo<TElement>(IEnumerable<TElement> expected, IEqualityComparer<TElement> comparer)
    {
        var expectedList = expected?.ToList() ?? new List<TElement>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                if (!(actual is IEnumerable<TElement> actualEnumerable)) return false;

                var actualList = actualEnumerable.ToList();
                if (actualList.Count != expectedList.Count) return false;

                // Check if all elements match using the custom comparer
                var expectedCopy = new List<TElement>(expectedList);
                foreach (var item in actualList)
                {
                    var matchIndex = expectedCopy.FindIndex(e => comparer.Equals(item, e));
                    if (matchIndex == -1) return false;
                    expectedCopy.RemoveAt(matchIndex);
                }
                return expectedCopy.Count == 0;
            },
            $"Expected collections to be equivalent using custom comparer");
    }

    // IsEquivalentTo with CollectionOrdering
    public CustomAssertion<TActual> IsEquivalentTo<TElement>(IEnumerable<TElement> expected, Enums.CollectionOrdering ordering)
    {
        var expectedList = expected?.ToList() ?? new List<TElement>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                if (!(actual is IEnumerable<TElement> actualEnumerable)) return false;

                var actualList = actualEnumerable.ToList();
                if (actualList.Count != expectedList.Count) return false;

                if (ordering == Enums.CollectionOrdering.Matching)
                {
                    // Elements must be in the same order
                    return actualList.SequenceEqual(expectedList);
                }
                else // CollectionOrdering.Any
                {
                    // Elements can be in any order - use the same logic as above
                    var comparer = EqualityComparer<TElement>.Default;
                    var expectedCopy = new List<TElement>(expectedList);
                    foreach (var item in actualList)
                    {
                        var matchIndex = expectedCopy.FindIndex(e => comparer.Equals(item, e));
                        if (matchIndex == -1) return false;
                        expectedCopy.RemoveAt(matchIndex);
                    }
                    return expectedCopy.Count == 0;
                }
            },
            ordering == Enums.CollectionOrdering.Matching ?
                "Expected collections to be equivalent with matching order" :
                "Expected collections to be equivalent in any order");
    }

    // Custom assertion
    public CustomAssertion<TActual> Satisfies(Func<TActual, bool> predicate, string? failureMessage = null)
    {
        return new CustomAssertion<TActual>(_actualValueProvider, predicate, failureMessage);
    }

    // Satisfies with property selector and assertion
    public PropertySatisfiesAssertion<TActual, TProperty> Satisfies<TProperty>(
        Func<TActual, TProperty> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        return new PropertySatisfiesAssertion<TActual, TProperty>(_actualValueProvider, propertySelector, assertionBuilder);
    }

    // Special overload for Task-returning properties - unwraps the Task
    public PropertySatisfiesAsyncAssertion<TActual, TProperty> Satisfies<TProperty>(
        Func<TActual, Task<TProperty>> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        // Create an async property selector that properly awaits
        return new PropertySatisfiesAsyncAssertion<TActual, TProperty>(_actualValueProvider, propertySelector, assertionBuilder);
    }

    // Satisfies with type conversion and assertion returning CustomAssertion
    public CustomAssertion<TActual> Satisfies<TConverted>(
        Func<TActual, TConverted> converter,
        Func<AssertionBuilder<TConverted>, CustomAssertion<TConverted>> assertionBuilder)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                try
                {
                    var converted = converter(actual);
                    var builder = new AssertionBuilder<TConverted>(() => Task.FromResult(converted));
                    var assertion = assertionBuilder(builder);
                    // For now, just check that conversion succeeded
                    return converted != null;
                }
                catch
                {
                    return false;
                }
            },
            "Expected value to satisfy the given condition");
    }

    // IsIn assertion - check if value is in a collection
    public CustomAssertion<TActual> IsIn(params TActual[] values)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => values?.Contains(actual) ?? false,
            $"Expected value to be one of: {string.Join(", ", values ?? Array.Empty<TActual>())}");
    }

    public CustomAssertion<TActual> IsIn(IEnumerable<TActual> values)
    {
        var valuesList = values?.ToList() ?? new List<TActual>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => valuesList.Contains(actual),
            $"Expected value to be in the collection");
    }

    public CustomAssertion<TActual> IsNotIn(params TActual[] values)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => !values?.Contains(actual) ?? true,
            $"Expected value to not be one of: {string.Join(", ", values ?? Array.Empty<TActual>())}");
    }

    // IsNotEquivalentTo for collections
    public CustomAssertion<TActual> IsNotEquivalentTo(TActual expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;
                if (!(actual is System.Collections.IEnumerable actualEnumerable)) return true;
                if (!(expected is System.Collections.IEnumerable expectedEnumerable)) return true;
                var actualList = actualEnumerable.Cast<object?>().ToList();
                var expectedList = expectedEnumerable.Cast<object?>().ToList();
                return !(actualList.Count == expectedList.Count &&
                       actualList.All(expectedList.Contains) &&
                       expectedList.All(actualList.Contains));
            },
            $"Expected collection to not be equivalent to the expected collection");
    }

    // IsNotEquivalentTo with CollectionOrdering
    public CustomAssertion<TActual> IsNotEquivalentTo<TElement>(IEnumerable<TElement> expected, Enums.CollectionOrdering ordering)
    {
        var expectedList = expected?.ToList() ?? new List<TElement>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;
                if (!(actual is IEnumerable<TElement> actualEnumerable)) return true;

                var actualList = actualEnumerable.ToList();
                if (actualList.Count != expectedList.Count) return true;

                if (ordering == Enums.CollectionOrdering.Matching)
                {
                    // Elements must NOT be in the same order
                    return !actualList.SequenceEqual(expectedList);
                }
                else // CollectionOrdering.Any
                {
                    // Elements must NOT match regardless of order
                    var comparer = EqualityComparer<TElement>.Default;
                    var expectedCopy = new List<TElement>(expectedList);
                    foreach (var item in actualList)
                    {
                        var matchIndex = expectedCopy.FindIndex(e => comparer.Equals(item, e));
                        if (matchIndex == -1) return true;
                        expectedCopy.RemoveAt(matchIndex);
                    }
                    return expectedCopy.Count > 0;
                }
            },
            ordering == Enums.CollectionOrdering.Matching ?
                "Expected collections to not be equivalent with matching order" :
                "Expected collections to not be equivalent in any order");
    }

    // IsNotEquivalentTo for object (handles anonymous types and cross-type comparisons)
    public CustomAssertion<TActual> IsNotEquivalentTo(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;
                // For now, just use equals - could be enhanced with deep property comparison
                return !actual.Equals(expected);
            },
            $"Expected {{0}} to not be equivalent to {expected}");
    }

    // Exception assertions - for delegates that should throw
    public ExceptionAssertion<TException> Throws<TException>()
        where TException : Exception
    {
        // The value provider should be a delegate that throws
        return new ExceptionAssertion<TException>(async () =>
        {
            var value = await _actualValueProvider();
            // If value is an Action or Func, execute it
            if (value is Action action)
            {
                action();
            }
            else if (value is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use Throws with {typeof(TActual).Name}. Use with Action or Func<Task>.");
            }
        });
    }

    public ExceptionAssertion Throws(Type exceptionType)
    {
        return new ExceptionAssertion(async () =>
        {
            var value = await _actualValueProvider();
            if (value is Action action)
            {
                action();
            }
            else if (value is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use Throws with {typeof(TActual).Name}. Use with Action or Func<Task>.");
            }
        }, exceptionType);
    }

    public ExceptionAssertion<TException> ThrowsException<TException>()
        where TException : Exception
    {
        return Throws<TException>();
    }

    // Non-generic version that catches any exception
    public ExceptionAssertion ThrowsException()
    {
        var assertion = new ExceptionAssertion(async () =>
        {
            var value = await _actualValueProvider();
            if (value is Action action)
            {
                action();
            }
            else if (value is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use ThrowsException with {typeof(TActual).Name}. Use with Action or Func<Task>.");
            }
        });
        return assertion;
    }

    public ExceptionAssertion<TException> ThrowsExactly<TException>()
        where TException : Exception
    {
        var assertion = new ExceptionAssertion<TException>(async () =>
        {
            var value = await _actualValueProvider();
            if (value is Action action)
            {
                action();
            }
            else if (value is Func<Task> asyncFunc)
            {
                await asyncFunc();
            }
            else
            {
                throw new InvalidOperationException($"Cannot use ThrowsExactly with {typeof(TActual).Name}. Use with Action or Func<Task>.");
            }
        }, typeof(TException));
        return assertion;
    }

    // For generic return type
    public ThrowsNothingAssertion<TResult> ThrowsNothing<TResult>()
    {
        return new ThrowsNothingAssertion<TResult>(async () => (object?)await _actualValueProvider());
    }

    // For non-generic case (returns actual type)
    public ThrowsNothingAssertion<TActual> ThrowsNothing()
    {
        return new ThrowsNothingAssertion<TActual>(async () => (object?)await _actualValueProvider());
    }

    // Generic version of IsTypeOf - returns TypeOfAssertion for chaining
    public TypeOfAssertion<TExpected> IsTypeOf<TExpected>()
        where TExpected : class
    {
        return new TypeOfAssertion<TExpected>(_actualValueProvider);
    }

    private async Task<TExpected> ExecuteTypeAssertionAndGetValue<TExpected>()
        where TExpected : class
    {
        var actual = await _actualValueProvider();

        if (actual == null)
        {
            throw new AssertionException($"Expected type {typeof(TExpected).Name} but was null");
        }

        var actualType = actual.GetType();
        if (actualType == typeof(TExpected))
        {
            return (TExpected)(object)actual;
        }

        throw new AssertionException($"Expected type exactly {typeof(TExpected).Name} but was {actualType.Name}");
    }

    // Generic version of IsAssignableTo
    public TypeAssertion<TActual> IsAssignableTo<TExpected>()
    {
        return new TypeAssertion<TActual>(_actualValueProvider, typeof(TExpected), exact: false);
    }

    // IsNotAssignableTo methods
    public CustomAssertion<TActual> IsNotAssignableTo(Type expectedType)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            value => value != null && !expectedType.IsAssignableFrom(value.GetType()),
            $"Expected value to not be assignable to {expectedType.Name}");
    }

    public CustomAssertion<TActual> IsNotAssignableTo<TExpected>()
    {
        return IsNotAssignableTo(typeof(TExpected));
    }
}

// Extension methods for reference assertions
public static class ReferenceAssertionBuilderExtensions
{
    public static ReferenceAssertion<TActual> IsSameReferenceAs<TActual>(this AssertionBuilder<TActual> builder, TActual? expected)
        where TActual : class
    {
        return new ReferenceAssertion<TActual>(builder.ActualValueProvider, expected, shouldBeSame: true);
    }

    public static ReferenceAssertion<TActual> IsNotSameReferenceAs<TActual>(this AssertionBuilder<TActual> builder, TActual? expected)
        where TActual : class
    {
        return new ReferenceAssertion<TActual>(builder.ActualValueProvider, expected, shouldBeSame: false);
    }
}

// Extension methods for object reference assertions (without constraint)
public static class ObjectReferenceAssertionExtensions
{
    public static CustomAssertion<TActual> IsNotSameReferenceAs<TActual>(this AssertionBuilder<TActual> builder, object? expected)
    {
        return new CustomAssertion<TActual>(builder.ActualValueProvider,
            actual => !ReferenceEquals(actual, expected),
            "Expected different reference");
    }

    public static CustomAssertion<TActual> IsSameReferenceAs<TActual>(this AssertionBuilder<TActual> builder, object? expected)
    {
        return new CustomAssertion<TActual>(builder.ActualValueProvider,
            actual => ReferenceEquals(actual, expected),
            "Expected same reference");
    }
}

// Extension methods for DateTime-specific assertions
public static class DateTimeAssertionBuilderExtensions
{
    public static DateTimeAssertion IsEqualTo(this ValueAssertionBuilder<DateTime> builder, DateTime expected)
    {
        return new DateTimeAssertion(builder.ActualValueProvider, expected);
    }

    public static CustomAssertion<DateTime> IsAfterOrEqualTo(this ValueAssertionBuilder<DateTime> builder, DateTime other)
    {
        return new CustomAssertion<DateTime>(builder.ActualValueProvider,
            dt => dt >= other,
            $"Expected DateTime to be after or equal to {other:O}");
    }

    public static CustomAssertion<DateTime> IsBeforeOrEqualTo(this ValueAssertionBuilder<DateTime> builder, DateTime other)
    {
        return new CustomAssertion<DateTime>(builder.ActualValueProvider,
            dt => dt <= other,
            $"Expected DateTime to be before or equal to {other:O}");
    }

    public static DateTimeOffsetAssertion IsEqualTo(this ValueAssertionBuilder<DateTimeOffset> builder, DateTimeOffset expected)
    {
        return new DateTimeOffsetAssertion(builder.ActualValueProvider, expected);
    }

    public static TimeSpanAssertion IsEqualTo(this ValueAssertionBuilder<TimeSpan> builder, TimeSpan expected)
    {
        return new TimeSpanAssertion(builder.ActualValueProvider, expected);
    }

#if NET6_0_OR_GREATER
    public static DateOnlyAssertion IsEqualTo(this ValueAssertionBuilder<DateOnly> builder, DateOnly expected)
    {
        return new DateOnlyAssertion(builder.ActualValueProvider, expected);
    }

    public static TimeOnlyAssertion IsEqualTo(this ValueAssertionBuilder<TimeOnly> builder, TimeOnly expected)
    {
        return new TimeOnlyAssertion(builder.ActualValueProvider, expected);
    }
#endif
}

// Extension methods for string-specific assertions
public static class StringAssertionBuilderExtensions
{
    // Use a single set of methods that work with string (non-nullable in practice)
    public static StringEqualToAssertion IsEqualTo(this AssertionBuilder<string> builder, string? expected)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringEqualToAssertion(nullableProvider, expected);
    }

    // Overload with StringComparison parameter
    public static StringEqualToAssertion IsEqualTo(this AssertionBuilder<string> builder, string? expected, StringComparison comparison)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringEqualToAssertion(nullableProvider, expected).WithStringComparison(comparison);
    }

    public static StringContainsAssertion Contains(this AssertionBuilder<string> builder, string substring)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringContainsAssertion(nullableProvider, substring);
    }

    public static StringStartsWithAssertion StartsWith(this AssertionBuilder<string> builder, string prefix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringStartsWithAssertion(nullableProvider, prefix);
    }

    public static StringEndsWithAssertion EndsWith(this AssertionBuilder<string> builder, string suffix)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new StringEndsWithAssertion(nullableProvider, suffix);
    }

    public static CustomAssertion<string> HasLength(this AssertionBuilder<string> builder, int expectedLength)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s?.Length == expectedLength,
            $"Expected string to have length {expectedLength}");
    }

    public static NumericAssertion<int> HasCount(this AssertionBuilder<string> builder)
    {
        Func<Task<int>> countProvider = async () =>
        {
            var str = await builder.ActualValueProvider();
            return str?.Length ?? 0;
        };
        return new NumericAssertion<int>(countProvider);
    }

    // HasLength that returns an AssertionBuilder<int> for further chaining
    public static AssertionBuilder<int> HasLength(this AssertionBuilder<string> builder)
    {
        return new AssertionBuilder<int>(async () =>
        {
            var str = await builder.ActualValueProvider();
            return str?.Length ?? 0;
        });
    }

    public static CustomAssertion<string> IsEmpty(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => string.IsNullOrEmpty(s),
            "Expected string to be empty");
    }

    public static CustomAssertion<string> IsNotEmpty(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => !string.IsNullOrEmpty(s),
            "Expected string to not be empty");
    }

    public static CustomAssertion<string> DoesNotContain(this AssertionBuilder<string> builder, string substring)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !s.Contains(substring),
            $"Expected string to not contain '{substring}'");
    }

    public static CustomAssertion<string> Matches(this AssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to match pattern '{pattern}'");
    }

    public static CustomAssertion<string> Matches(this AssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != null && regex.IsMatch(s),
            $"Expected string to match regex pattern");
    }

    public static CustomAssertion<string> DoesNotMatch(this AssertionBuilder<string> builder, string pattern)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !System.Text.RegularExpressions.Regex.IsMatch(s, pattern),
            $"Expected string to not match pattern '{pattern}'");
    }

    public static CustomAssertion<string> DoesNotMatch(this AssertionBuilder<string> builder, System.Text.RegularExpressions.Regex regex)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == null || !regex.IsMatch(s),
            $"Expected string to not match regex pattern");
    }

    public static CustomAssertion<string> IsNullOrEmpty(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => string.IsNullOrEmpty(s),
            "Expected string to be null or empty");
    }

    public static CustomAssertion<string> IsNotNullOrEmpty(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => !string.IsNullOrEmpty(s),
            "Expected string to not be null or empty");
    }

    public static CustomAssertion<string> IsNullOrWhitespace(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => string.IsNullOrWhiteSpace(s),
            "Expected string to be null or whitespace");
    }

    public static CustomAssertion<string> IsDefault(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s == default(string),
            "Expected string to be default (null)");
    }

    public static CustomAssertion<string> IsNotDefault(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            s => s != default(string),
            "Expected string to not be default (null)");
    }

    // File system assertions for string paths
    public static CustomAssertion<string> Exists(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            path => path != null && (System.IO.File.Exists(path) || System.IO.Directory.Exists(path)),
            "Expected path to exist");
    }

    public static CustomAssertion<string> DoesNotExist(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            path => path == null || (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path)),
            "Expected path to not exist");
    }

    public static CustomAssertion<string> HasFiles(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            path => path != null && System.IO.Directory.Exists(path) && System.IO.Directory.GetFiles(path).Length > 0,
            "Expected directory to have files");
    }

    public static CustomAssertion<string> HasNoSubdirectories(this AssertionBuilder<string> builder)
    {
        return new CustomAssertion<string>(builder.ActualValueProvider,
            path => path != null && System.IO.Directory.Exists(path) && System.IO.Directory.GetDirectories(path).Length == 0,
            "Expected directory to have no subdirectories");
    }

#if !NET7_0_OR_GREATER
    // Parse assertions for older frameworks using TryParse patterns
    public static ParseAssertion<T> IsParsableInto<T>(this AssertionBuilder<string> builder)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: true);
    }

    public static ParseAssertion<T> IsNotParsableInto<T>(this AssertionBuilder<string> builder)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: false);
    }

    public static WhenParsedAssertion<T> WhenParsedInto<T>(this AssertionBuilder<string> builder)
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new WhenParsedAssertion<T>(nullableProvider);
    }
#else
    // Parse assertions for .NET 7+ (which has IParsable<T>)
    public static ParseAssertion<T> IsParsableInto<T>(this AssertionBuilder<string> builder)
        where T : IParsable<T>
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: true);
    }

    public static ParseAssertion<T> IsNotParsableInto<T>(this AssertionBuilder<string> builder)
        where T : IParsable<T>
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new ParseAssertion<T>(nullableProvider, shouldBeParsable: false);
    }

    public static WhenParsedAssertion<T> WhenParsedInto<T>(this AssertionBuilder<string> builder)
        where T : IParsable<T>
    {
        Func<Task<string?>> nullableProvider = async () => await builder.ActualValueProvider();
        return new WhenParsedAssertion<T>(nullableProvider);
    }
#endif
}

// Extension methods for nullable string-specific assertions
public static class NullableStringAssertionBuilderExtensions
{
    // Removed duplicate methods that conflict with StringAssertionBuilderExtensions
    // The StringAssertionBuilderExtensions already handle nullable strings properly

    public static CustomAssertion<string?> HasLength(this AssertionBuilder<string?> builder, int expectedLength)
    {
        return new CustomAssertion<string?>(builder.ActualValueProvider,
            s => s?.Length == expectedLength,
            $"Expected string to have length {expectedLength}");
    }

    public static CustomAssertion<string?> IsEmpty(this AssertionBuilder<string?> builder)
    {
        return new CustomAssertion<string?>(builder.ActualValueProvider,
            s => string.IsNullOrEmpty(s),
            "Expected string to be empty");
    }

    public static CustomAssertion<string?> IsNotEmpty(this AssertionBuilder<string?> builder)
    {
        return new CustomAssertion<string?>(builder.ActualValueProvider,
            s => !string.IsNullOrEmpty(s),
            "Expected string to not be empty");
    }

    public static CustomAssertion<string?> DoesNotContain(this AssertionBuilder<string?> builder, string substring)
    {
        return new CustomAssertion<string?>(builder.ActualValueProvider,
            s => s == null || !s.Contains(substring),
            $"Expected string to not contain '{substring}'");
    }

}

// Extension methods for CultureInfo assertions
public static class CultureInfoAssertionExtensions
{
    public static CustomAssertion<System.Globalization.CultureInfo> IsInvariant(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci != null && ci.Equals(System.Globalization.CultureInfo.InvariantCulture),
            "Expected CultureInfo to be InvariantCulture");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsNotInvariant(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci != null && !ci.Equals(System.Globalization.CultureInfo.InvariantCulture),
            "Expected CultureInfo not to be InvariantCulture");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsNeutralCulture(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.IsNeutralCulture ?? false,
            "Expected CultureInfo to be a neutral culture");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsNotNeutralCulture(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci == null || !ci.IsNeutralCulture,
            "Expected CultureInfo to not be a neutral culture");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsReadOnly(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.IsReadOnly ?? false,
            "Expected CultureInfo to be read-only");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsEnglish(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.TwoLetterISOLanguageName == "en",
            "Expected CultureInfo to be English");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsNotEnglish(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.TwoLetterISOLanguageName != "en",
            "Expected CultureInfo to not be English");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsLeftToRight(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.TextInfo?.IsRightToLeft == false,
            "Expected CultureInfo to be left-to-right");
    }

    public static CustomAssertion<System.Globalization.CultureInfo> IsRightToLeft(this ValueAssertionBuilder<System.Globalization.CultureInfo> builder)
    {
        return new CustomAssertion<System.Globalization.CultureInfo>(builder.ActualValueProvider,
            ci => ci?.TextInfo?.IsRightToLeft == true,
            "Expected CultureInfo to be right-to-left");
    }
}