using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks if all items in a collection satisfy a property condition
/// </summary>
public class PropertySatisfiesAllAssertion<T, TProperty> : AssertionBase<IEnumerable<T>>
{
    private readonly Func<T, TProperty> _propertySelector;
    private readonly Func<AssertionBuilder<TProperty>, AssertionBase> _assertionBuilder;

    public PropertySatisfiesAllAssertion(
        Func<Task<IEnumerable<T>>> actualValueProvider,
        Func<T, TProperty> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _propertySelector = propertySelector;
        _assertionBuilder = assertionBuilder;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var collection = await GetActualValueAsync();

        if (collection == null)
        {
            return AssertionResult.Fail("Collection was null");
        }

        foreach (var item in collection)
        {
            var propertyValue = _propertySelector(item);
            var builder = new AssertionBuilder<TProperty>(() => Task.FromResult(propertyValue));
            var assertion = _assertionBuilder(builder);

            try
            {
                await assertion.ExecuteAsync();
            }
            catch (Exception ex)
            {
                return AssertionResult.Fail($"Item failed assertion: {ex.Message}");
            }
        }

        return AssertionResult.Passed;
    }
}