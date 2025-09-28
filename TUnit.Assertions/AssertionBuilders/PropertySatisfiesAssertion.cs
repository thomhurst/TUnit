using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks if a property satisfies a given condition
/// </summary>
public class PropertySatisfiesAssertion<TActual, TProperty> : AssertionBase<TActual>
{
    private readonly Func<TActual, TProperty> _propertySelector;
    private readonly Func<AssertionBuilder<TProperty>, AssertionBase> _assertionBuilder;

    public PropertySatisfiesAssertion(
        Func<Task<TActual>> actualValueProvider,
        Func<TActual, TProperty> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _propertySelector = propertySelector;
        _assertionBuilder = assertionBuilder;
    }

    public PropertySatisfiesAssertion(
        Func<TActual> actualValueProvider,
        Func<TActual, TProperty> propertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _propertySelector = propertySelector;
        _assertionBuilder = assertionBuilder;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail("Expected value but was null");
        }

        var propertyValue = _propertySelector(actual);
        var builder = new AssertionBuilder<TProperty>(() => Task.FromResult(propertyValue));
        var propertyAssertion = _assertionBuilder(builder);

        try
        {
            await propertyAssertion.ExecuteAsync();
            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Fail($"Property assertion failed: {ex.Message}");
        }
    }
}