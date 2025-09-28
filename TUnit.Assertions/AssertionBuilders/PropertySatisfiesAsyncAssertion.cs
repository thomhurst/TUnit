using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks if an async property satisfies a given condition
/// </summary>
public class PropertySatisfiesAsyncAssertion<TActual, TProperty> : AssertionBase<TActual>
{
    private readonly Func<TActual, Task<TProperty>> _asyncPropertySelector;
    private readonly Func<AssertionBuilder<TProperty>, AssertionBase> _assertionBuilder;

    public PropertySatisfiesAsyncAssertion(
        Func<Task<TActual>> actualValueProvider,
        Func<TActual, Task<TProperty>> asyncPropertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _asyncPropertySelector = asyncPropertySelector;
        _assertionBuilder = assertionBuilder;
    }

    public PropertySatisfiesAsyncAssertion(
        Func<TActual> actualValueProvider,
        Func<TActual, Task<TProperty>> asyncPropertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _asyncPropertySelector = asyncPropertySelector;
        _assertionBuilder = assertionBuilder;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail("Expected value but was null");
        }

        // Await the async property selector
        var propertyTask = _asyncPropertySelector(actual);
        if (propertyTask == null)
        {
            return AssertionResult.Fail("Property selector returned null task");
        }

        var propertyValue = await propertyTask;

        // Create assertion builder with the resolved value
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