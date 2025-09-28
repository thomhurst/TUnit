using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for checking all items in a collection
/// </summary>
public class CollectionAllAssertion<T> : AssertionBase<IEnumerable<T>>
{
    public CollectionAllAssertion(Func<Task<IEnumerable<T>>> actualValueProvider)
        : base(actualValueProvider)
    {
    }

    public CollectionAllAssertion(Func<IEnumerable<T>> actualValueProvider)
        : base(actualValueProvider)
    {
    }

    // Satisfy method for asserting on all items with property selector
    public PropertySatisfiesAllAssertion<T, TProperty> Satisfy<TProperty>(
        Func<T, TProperty> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        return new PropertySatisfiesAllAssertion<T, TProperty>(GetActualValueAsync, propertySelector, assertionBuilder);
    }

    // Satisfy method for asserting directly on all items
    public PropertySatisfiesAllAssertion<T, T> Satisfy(Func<AssertionBuilder<T>, AssertionBase> assertionBuilder)
    {
        return new PropertySatisfiesAllAssertion<T, T>(GetActualValueAsync, item => item, assertionBuilder);
    }

    // Satisfy method for asserting on all items with async property selector
    public PropertySatisfiesAllAsyncAssertion<T, TProperty> Satisfy<TProperty>(
        Func<T, Task<TProperty>> asyncPropertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
    {
        return new PropertySatisfiesAllAsyncAssertion<T, TProperty>(GetActualValueAsync, asyncPropertySelector, assertionBuilder);
    }

    // Satisfy with predicate
    public CustomAssertion<IEnumerable<T>> Satisfy(Func<T, bool> predicate)
    {
        return new CustomAssertion<IEnumerable<T>>(GetActualValueAsync,
            collection => collection?.All(predicate) ?? true,
            "Expected all items to satisfy the predicate");
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var collection = await GetActualValueAsync();
        if (collection == null)
        {
            return AssertionResult.Fail("Collection was null");
        }

        return AssertionResult.Passed;
    }
}