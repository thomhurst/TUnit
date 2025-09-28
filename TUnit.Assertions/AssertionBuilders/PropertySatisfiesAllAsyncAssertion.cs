using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks if all items' async properties satisfy a condition
/// </summary>
public class PropertySatisfiesAllAsyncAssertion<T, TProperty> : AssertionBase<IEnumerable<T>>
{
    private readonly Func<T, Task<TProperty>> _asyncPropertySelector;
    private readonly Func<AssertionBuilder<TProperty>, AssertionBase> _assertionBuilder;

    public PropertySatisfiesAllAsyncAssertion(
        Func<Task<IEnumerable<T>>> actualValueProvider,
        Func<T, Task<TProperty>> asyncPropertySelector,
        Func<AssertionBuilder<TProperty>, AssertionBase> assertionBuilder)
        : base(actualValueProvider)
    {
        _asyncPropertySelector = asyncPropertySelector;
        _assertionBuilder = assertionBuilder;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var items = await GetActualValueAsync();

        if (items == null)
        {
            return AssertionResult.Fail("Expected collection but was null");
        }

        var itemsList = items.ToList();
        if (!itemsList.Any())
        {
            return AssertionResult.Passed; // Empty collection satisfies vacuously
        }

        var failures = new List<string>();
        var index = 0;

        foreach (var item in itemsList)
        {
            try
            {
                // Await the async property selector
                var propertyTask = _asyncPropertySelector(item);
                if (propertyTask == null)
                {
                    failures.Add($"Item at index {index}: property selector returned null task");
                    index++;
                    continue;
                }

                var propertyValue = await propertyTask;

                var builder = new AssertionBuilder<TProperty>(() => Task.FromResult(propertyValue));
                var assertion = _assertionBuilder(builder);

                await assertion.ExecuteAsync();
            }
            catch (Exception ex)
            {
                failures.Add($"Item at index {index}: {ex.Message}");
            }

            index++;
        }

        if (failures.Count > 0)
        {
            return AssertionResult.Fail($"Not all items satisfied the assertion:\n{string.Join("\n", failures)}");
        }

        return AssertionResult.Passed;
    }
}