using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for checking property members and allowing chained value assertions
/// </summary>
public class PropertyMemberAssertion<TObject, TProperty> : AssertionBase<TObject>
{
    private readonly Func<TObject, TProperty> _propertySelector;
    private TProperty? _propertyValue;

    public PropertyMemberAssertion(Func<Task<TObject>> actualValueProvider, Func<TObject, TProperty> propertySelector)
        : base(actualValueProvider)
    {
        _propertySelector = propertySelector;
    }

    public PropertyMemberAssertion(Func<TObject> actualValueProvider, Func<TObject, TProperty> propertySelector)
        : base(actualValueProvider)
    {
        _propertySelector = propertySelector;
    }

    // Allow chaining with EqualTo
    public CustomAssertion<TObject> EqualTo(TProperty expectedValue)
    {
        return new CustomAssertion<TObject>(GetActualValueAsync,
            obj =>
            {
                if (obj == null) return false;
                try
                {
                    var actualValue = _propertySelector(obj);
                    return object.Equals(actualValue, expectedValue);
                }
                catch
                {
                    return false;
                }
            },
            $"Expected property to equal {expectedValue}");
    }

    // Allow chaining with IsEqualTo
    public CustomAssertion<TObject> IsEqualTo(TProperty expectedValue)
    {
        return EqualTo(expectedValue);
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var obj = await GetActualValueAsync();
        if (obj == null)
        {
            return AssertionResult.Fail("Object was null");
        }

        try
        {
            _propertyValue = _propertySelector(obj);
            return AssertionResult.Passed;
        }
        catch (Exception ex)
        {
            return AssertionResult.Fail($"Failed to access property: {ex.Message}");
        }
    }
}