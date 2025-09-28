using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Base assertion builder for value assertions that work on any type
/// </summary>
public class ValueAssertionBuilder<TActual> : AssertionBuilder
{
    protected readonly Func<Task<TActual>> _actualValueProvider;
    protected readonly string? _expression;

    internal Func<Task<TActual>> ActualValueProvider => _actualValueProvider;

    public ValueAssertionBuilder<TActual> And => this;
    public ValueAssertionBuilder<TActual> Or => this;

    public TaskAwaiter<TActual> GetTypedAwaiter()
    {
        return _actualValueProvider().GetAwaiter();
    }

    public ValueAssertionBuilder(TActual value, string? expression = null)
        : this(() => Task.FromResult(value), expression)
    {
    }

    public ValueAssertionBuilder(Func<TActual> valueProvider, string? expression = null)
        : this(() => Task.FromResult(valueProvider()), expression)
    {
    }

    public ValueAssertionBuilder(Func<Task<TActual>> asyncValueProvider, string? expression = null)
    {
        _actualValueProvider = asyncValueProvider;
        _expression = expression;
    }

    public ValueAssertionBuilder(Task<TActual> task, string? expression = null)
        : this(() => task, expression)
    {
    }

    public ValueAssertionBuilder(ValueTask<TActual> valueTask, string? expression = null)
        : this(() => valueTask.AsTask(), expression)
    {
    }

    public GenericEqualToAssertion<TActual> IsEqualTo(TActual expected)
    {
        return new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

    public GenericEqualToAssertion<TActual> IsEqualTo(TActual expected, string because)
    {
        var assertion = new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
        assertion.Because(because);
        return assertion;
    }

        public CustomAssertion<TActual> IsEqualTo(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => object.Equals(actual, expected),
            $"Expected {expected} but was {{ActualValue}}");
    }

        public CustomAssertion<TActual> IsEqualTo(TActual expected, IEqualityComparer<TActual> comparer)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => comparer.Equals(actual, expected),
            $"Expected {expected} using custom comparer but was {{ActualValue}}");
    }

        public GenericEqualToAssertion<TActual> EqualTo(TActual expected)
    {
        return IsEqualTo(expected);
    }

        public GenericEqualToAssertion<TActual> IsEquatableOrEqualTo(TActual expected)
    {
        return new GenericEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

    public GenericNotEqualToAssertion<TActual> IsNotEqualTo(TActual expected)
    {
        return new GenericNotEqualToAssertion<TActual>(_actualValueProvider, expected);
    }

        public CustomAssertion<TActual> IsNotEqualTo(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;
                return !actual.Equals(expected);
            },
            $"Expected {{0}} to not be equal to {expected}");
    }

        public NullAssertion<TActual> IsNull()
    {
        return new NullAssertion<TActual>(_actualValueProvider, shouldBeNull: true);
    }

    public NullAssertion<TActual> IsNotNull()
    {
        return new NullAssertion<TActual>(_actualValueProvider, shouldBeNull: false);
    }

        public CustomAssertion<TActual> IsSameReferenceAs(TActual expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => object.ReferenceEquals(actual, expected),
            $"Expected same reference as {expected} but was {{ActualValue}}");
    }

        public CustomAssertion<TActual> IsSameReferenceAs(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => object.ReferenceEquals(actual, expected),
            $"Expected same reference as {expected} but was {{ActualValue}}");
    }

    public CustomAssertion<TActual> IsNotSameReferenceAs(TActual expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => !object.ReferenceEquals(actual, expected),
            $"Expected different reference than {expected} but was same reference");
    }

        public CustomAssertion<TActual> IsNotSameReferenceAs(object? expected)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => !object.ReferenceEquals(actual, expected),
            $"Expected different reference than {expected} but was same reference");
    }

        public TypeAssertion<TActual> IsTypeOf(Type expectedType)
    {
        return new TypeAssertion<TActual>(_actualValueProvider, expectedType, exact: true);
    }

    public TypeOfAssertion<TExpected> IsTypeOf<TExpected>()
        where TExpected : class
    {
        return new TypeOfAssertion<TExpected>(async () =>
        {
            var result = await _actualValueProvider();
            return result as object ?? throw new InvalidOperationException("Value was null");
        });
    }

    public TypeAssertion<TActual> IsAssignableTo(Type expectedType)
    {
        return new TypeAssertion<TActual>(_actualValueProvider, expectedType, exact: false);
    }

    public TypeAssertion<TActual> IsAssignableTo<TExpected>()
    {
        return IsAssignableTo(typeof(TExpected));
    }

    public CustomAssertion<TActual> IsNotAssignableTo(Type expectedType)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => actual == null || !expectedType.IsAssignableFrom(actual.GetType()),
            $"Expected type to not be assignable to {expectedType.Name} but was {{ActualValue?.GetType().Name}}");
    }

    public CustomAssertion<TActual> IsNotAssignableTo<TExpected>()
    {
        return IsNotAssignableTo(typeof(TExpected));
    }

        public CustomAssertion<TActual> Satisfies(Func<TActual, bool> predicate, string? failureMessage = null)
    {
        return new CustomAssertion<TActual>(_actualValueProvider, predicate, failureMessage ?? "Expected value to satisfy the predicate");
    }

        public PropertySatisfiesAssertion<TActual, TProperty> Satisfies<TProperty>(
        System.Linq.Expressions.Expression<Func<TActual, TProperty>> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        var compiled = propertySelector.Compile();
        return new PropertySatisfiesAssertion<TActual, TProperty>(_actualValueProvider, compiled, assertionBuilder);
    }

    public PropertySatisfiesAsyncAssertion<TActual, TProperty> Satisfies<TProperty>(
        System.Linq.Expressions.Expression<Func<TActual, Task<TProperty>>> asyncPropertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        var compiled = asyncPropertySelector.Compile();
        return new PropertySatisfiesAsyncAssertion<TActual, TProperty>(_actualValueProvider, compiled, assertionBuilder);
    }

        public CustomAssertion<TActual> Satisfies<TConverted>(
        Func<TActual, TConverted> converter,
        Func<ValueAssertionBuilder<TConverted>, CustomAssertion<TConverted>> assertionBuilder)
    {
        return new CustomAssertion<TActual>(_actualValueProvider, actual =>
        {
            var converted = converter(actual);
            var builder = new ValueAssertionBuilder<TConverted>(converted);
            var assertion = assertionBuilder(builder);

            return true;
        }, "Satisfies assertion failed");
    }

        public CustomAssertion<TActual> IsEquivalentTo(object? expected, Func<object, object, bool>? comparer = null)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;

                                if (actual is System.Collections.IEnumerable actualEnumerable &&
                    expected is System.Collections.IEnumerable expectedEnumerable)
                {
                    var actualList = actualEnumerable.Cast<object?>().ToList();
                    var expectedList = expectedEnumerable.Cast<object?>().ToList();
                    return actualList.Count == expectedList.Count &&
                           actualList.All(expectedList.Contains) &&
                           expectedList.All(actualList.Contains);
                }

                                if (comparer != null)
                {
                    return comparer(actual, expected);
                }

                                return DeepEquals(actual, expected);
            },
            $"Expected object to be equivalent to {expected}");
    }

        public CustomAssertion<TActual> IsEquivalentTo(IEnumerable<object?> expected)
    {
        return IsEquivalentTo((object?)expected, null);
    }

        public CustomAssertion<TActual> IsEquivalentTo<TExpected>(IEnumerable<TExpected> expected, Enums.CollectionOrdering ordering)
    {
        if (ordering == Enums.CollectionOrdering.Matching)
        {
                        return new CustomAssertion<TActual>(_actualValueProvider,
                actual =>
                {
                    if (actual == null && expected == null) return true;
                    if (actual == null || expected == null) return false;

                    if (actual is System.Collections.IEnumerable actualEnumerable)
                    {
                        var actualList = actualEnumerable.Cast<object?>().ToList();
                        var expectedList = expected.Cast<object?>().ToList();

                        if (actualList.Count != expectedList.Count) return false;

                        for (int i = 0; i < actualList.Count; i++)
                        {
                            if (!object.Equals(actualList[i], expectedList[i]))
                                return false;
                        }
                        return true;
                    }
                    return false;
                },
                $"Expected collection to be equivalent to the expected collection with matching order");
        }
        else
        {
                        return IsEquivalentTo((object?)expected, null);
        }
    }

        public CustomAssertion<TActual> IsEquivalentTo<TElement>(IEnumerable<TElement> expected, IEqualityComparer<TElement> comparer)
    {
        var expectedList = expected?.ToList() ?? new List<TElement>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expectedList.Count == 0) return true;
                if (actual == null) return false;

                if (actual is not IEnumerable<TElement> actualEnumerable)
                    return false;

                var actualList = actualEnumerable.ToList();
                if (actualList.Count != expectedList.Count) return false;

                                foreach (var expectedItem in expectedList)
                {
                    bool found = actualList.Any(actualItem => comparer.Equals(actualItem, expectedItem));
                    if (!found) return false;
                }

                                foreach (var actualItem in actualList)
                {
                    bool found = expectedList.Any(expectedItem => comparer.Equals(actualItem, expectedItem));
                    if (!found) return false;
                }

                return true;
            },
            "Expected collections to be equivalent using custom comparer");
    }

    // Collection equivalence with ordering option (object version)
    public CustomAssertion<TActual> IsEquivalentTo(IEnumerable<object?> expected, Enums.CollectionOrdering ordering)
    {
        if (ordering == Enums.CollectionOrdering.Matching)
        {
                        return new CustomAssertion<TActual>(_actualValueProvider,
                actual =>
                {
                    if (actual == null && expected == null) return true;
                    if (actual == null || expected == null) return false;

                    if (actual is System.Collections.IEnumerable actualEnumerable)
                    {
                        var actualList = actualEnumerable.Cast<object?>().ToList();
                        var expectedList = expected.ToList();

                        if (actualList.Count != expectedList.Count) return false;

                        for (int i = 0; i < actualList.Count; i++)
                        {
                            if (!object.Equals(actualList[i], expectedList[i]))
                                return false;
                        }
                        return true;
                    }
                    return false;
                },
                $"Expected collection to be equivalent to the expected collection with matching order");
        }
        else
        {
                        return IsEquivalentTo((object?)expected, null);
        }
    }

    // IsNotEquivalentTo for objects with options
    public CustomAssertion<TActual> IsNotEquivalentTo(object? expected, Func<object, object, bool>? comparer = null)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return false;
                if (actual == null || expected == null) return true;

                                if (actual is System.Collections.IEnumerable actualEnumerable &&
                    expected is System.Collections.IEnumerable expectedEnumerable)
                {
                    var actualList = actualEnumerable.Cast<object?>().ToList();
                    var expectedList = expectedEnumerable.Cast<object?>().ToList();
                    return actualList.Count != expectedList.Count ||
                           !actualList.All(expectedList.Contains) ||
                           !expectedList.All(actualList.Contains);
                }

                                if (comparer != null)
                {
                    return !comparer(actual, expected);
                }

                                return !DeepEquals(actual, expected);
            },
            $"Expected object to not be equivalent to {expected}");
    }

    // IsNotEquivalentTo for collections/objects
    public CustomAssertion<TActual> IsNotEquivalentTo(IEnumerable<object?> expected)
    {
        return IsNotEquivalentTo((object?)expected, null);
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
            $"Expected value to be one of: {string.Join(", ", valuesList)}");
    }

    public CustomAssertion<TActual> IsNotIn(params TActual[] values)
    {
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => !(values?.Contains(actual) ?? false),
            $"Expected value to not be one of: {string.Join(", ", values ?? Array.Empty<TActual>())}");
    }

    public CustomAssertion<TActual> IsNotIn(IEnumerable<TActual> values)
    {
        var valuesList = values?.ToList() ?? new List<TActual>();
        return new CustomAssertion<TActual>(_actualValueProvider,
            actual => !valuesList.Contains(actual),
            $"Expected value to not be one of: {string.Join(", ", valuesList)}");
    }

    // Generic IsNotEquivalentTo with ordering option
    public CustomAssertion<TActual> IsNotEquivalentTo<TExpected>(IEnumerable<TExpected> expected, Enums.CollectionOrdering ordering)
    {
        if (ordering == Enums.CollectionOrdering.Matching)
        {
                        return new CustomAssertion<TActual>(_actualValueProvider,
                actual =>
                {
                    if (actual == null && expected == null) return false;
                    if (actual == null || expected == null) return true;

                    if (actual is System.Collections.IEnumerable actualEnumerable)
                    {
                        var actualList = actualEnumerable.Cast<object?>().ToList();
                        var expectedList = expected.Cast<object?>().ToList();

                        if (actualList.Count != expectedList.Count) return true;

                        for (int i = 0; i < actualList.Count; i++)
                        {
                            if (!object.Equals(actualList[i], expectedList[i]))
                                return true;
                        }
                        return false;
                    }
                    return true;
                },
                $"Expected collection to not be equivalent to the expected collection with matching order");
        }
        else
        {
                        return IsNotEquivalentTo((object?)expected, null);
        }
    }

    // IsNotEquivalentTo with ordering option (object version)
    public CustomAssertion<TActual> IsNotEquivalentTo(IEnumerable<object?> expected, Enums.CollectionOrdering ordering)
    {
        if (ordering == Enums.CollectionOrdering.Matching)
        {
                        return new CustomAssertion<TActual>(_actualValueProvider,
                actual =>
                {
                    if (actual == null && expected == null) return false;
                    if (actual == null || expected == null) return true;

                    if (actual is System.Collections.IEnumerable actualEnumerable)
                    {
                        var actualList = actualEnumerable.Cast<object?>().ToList();
                        var expectedList = expected.ToList();

                        if (actualList.Count != expectedList.Count) return true;

                        for (int i = 0; i < actualList.Count; i++)
                        {
                            if (!object.Equals(actualList[i], expectedList[i]))
                                return true;
                        }
                        return false;
                    }
                    return true;
                },
                $"Expected collection to not be equivalent to the expected collection with matching order");
        }
        else
        {
                        return IsNotEquivalentTo((object?)expected, null);
        }
    }

    // Helper method for deep equality comparison
    private static bool DeepEquals(object? actual, object? expected)
    {
        if (actual == null && expected == null) return true;
        if (actual == null || expected == null) return false;
        if (actual.GetType() != expected.GetType()) return false;

        // For now, use default equality - could be enhanced with reflection-based property comparison
        return actual.Equals(expected);
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

    // Abstract base class implementations - using typed version
    public override TaskAwaiter GetAwaiter()
    {
        return ((Task)_actualValueProvider()).GetAwaiter();
    }

    public override ValueTask<AssertionData> GetAssertionData()
    {
        return new ValueTask<AssertionData>(new AssertionData(null, null, _expression, DateTimeOffset.Now, DateTimeOffset.Now));
    }

    public override ValueTask ProcessAssertionsAsync(AssertionData data)
    {
        return new ValueTask();
    }
}